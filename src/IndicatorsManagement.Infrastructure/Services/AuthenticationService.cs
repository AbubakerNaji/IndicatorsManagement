using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Infrastructure.Data;
using IndicatorsManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace IndicatorsManagement.Infrastructure.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IndicatorsDbContext _db;
    private readonly IAuditLogService _audit;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        IndicatorsDbContext db,
        IAuditLogService audit)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _db = db;
        _audit = audit;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent)
    {
        // Find user by username or email
        var user = await _userManager.FindByNameAsync(request.UserNameOrEmail)
            ?? await _userManager.FindByEmailAsync(request.UserNameOrEmail);

        if (user is null)
        {
            await _audit.LogAsync(null, "User", null, "Login_Failed",
                ipAddress: ipAddress, resultStatus: "Failure",
                errorMessage: $"User not found: {request.UserNameOrEmail}");
            return ApiResponse<LoginResponse>.Fail("اسم المستخدم أو كلمة المرور غير صحيحة");
        }

        if (!user.IsActive)
        {
            await _audit.LogAsync(user.Id, "User", user.Id, "Login_Failed_Inactive",
                ipAddress: ipAddress, resultStatus: "Failure",
                errorMessage: "Account is deactivated");
            return ApiResponse<LoginResponse>.Fail("الحساب معطّل. يرجى التواصل مع المسؤول");
        }

        // Check if account is locked out
        if (await _userManager.IsLockedOutAsync(user))
        {
            await _audit.LogAsync(user.Id, "User", user.Id, "Login_Failed_Locked",
                ipAddress: ipAddress, resultStatus: "Failure");
            return ApiResponse<LoginResponse>.Fail("الحساب مقفل مؤقتاً بسبب محاولات تسجيل دخول متعددة فاشلة. يرجى المحاولة بعد 15 دقيقة");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            var errorMsg = result.IsLockedOut
                ? "Account locked after failed attempts"
                : "Invalid password";

            await _audit.LogAsync(user.Id, "User", user.Id, "Login_Failed",
                ipAddress: ipAddress, resultStatus: "Failure", errorMessage: errorMsg);

            if (result.IsLockedOut)
                return ApiResponse<LoginResponse>.Fail("الحساب مقفل مؤقتاً بسبب محاولات تسجيل دخول متعددة فاشلة. يرجى المحاولة بعد 15 دقيقة");

            return ApiResponse<LoginResponse>.Fail("اسم المستخدم أو كلمة المرور غير صحيحة");
        }

        // Get user role
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        // Load entity name if applicable
        string? entityNameAr = null;
        if (user.EntityId.HasValue)
        {
            entityNameAr = await _db.Entities
                .Where(e => e.Id == user.EntityId.Value)
                .Select(e => e.NameAr)
                .FirstOrDefaultAsync();
        }

        // Generate JWT token
        var token = GenerateJwtToken(user, role);
        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "30");
        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

        // Create session — S2: store SHA-256 hash of the JWT, never the raw token.
        var session = new UserSession
        {
            UserId = user.Id,
            SessionToken = SessionTokenHasher.Hash(token),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            LastActivity = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync();

        // Reset failed access count on successful login
        await _userManager.ResetAccessFailedCountAsync(user);

        await _audit.LogAsync(user.Id, "User", user.Id, "Login_Success", ipAddress: ipAddress);

        return ApiResponse<LoginResponse>.Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserInfo
            {
                Id = user.Id,
                UserName = user.UserName!,
                FullNameAr = user.FullNameAr,
                Email = user.Email!,
                Role = role,
                EntityId = user.EntityId,
                EntityNameAr = entityNameAr
            }
        });
    }

    public async Task<ApiResponse> LogoutAsync(int userId, string sessionToken)
    {
        // S2 — the DB stores hashes, so hash the incoming raw token before lookup.
        var hashed = SessionTokenHasher.Hash(sessionToken);
        var session = await _db.UserSessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionToken == hashed);

        if (session is not null)
        {
            _db.UserSessions.Remove(session);
            await _db.SaveChangesAsync();
        }

        await _audit.LogAsync(userId, "User", userId, "Logout");

        return ApiResponse.Ok("تم تسجيل الخروج بنجاح");
    }

    public async Task<ApiResponse> AdminResetPasswordAsync(AdminResetPasswordRequest request, int adminUserId)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
            return ApiResponse.Fail("المستخدم غير موجود");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ApiResponse.Fail("فشل إعادة تعيين كلمة المرور", errors);
        }

        // Unlock account if locked
        await _userManager.SetLockoutEndDateAsync(user, null);

        await _audit.LogAsync(adminUserId, "User", user.Id, "Admin_Password_Reset",
            newValues: JsonSerializer.Serialize(new { TargetUserId = user.Id }));

        return ApiResponse.Ok("تم إعادة تعيين كلمة المرور بنجاح");
    }

    private string GenerateJwtToken(ApplicationUser user, string role)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Role, role),
            new("FullNameAr", user.FullNameAr)
        };

        if (user.EntityId.HasValue)
            claims.Add(new Claim("EntityId", user.EntityId.Value.ToString()));

        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "30");

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
