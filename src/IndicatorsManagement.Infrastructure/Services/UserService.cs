using System.Text.Json;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IndicatorsDbContext _db;
    private readonly IAuditLogService _audit;

    public UserService(UserManager<ApplicationUser> userManager, IndicatorsDbContext db, IAuditLogService audit)
    {
        _userManager = userManager;
        _db = db;
        _audit = audit;
    }

    public async Task<ApiResponse<PaginatedResponse<UserInfo>>> GetUsersAsync(int? entityId, int page = 1, int pageSize = 20)
    {
        var query = _db.Users.AsNoTracking().Where(u => u.IsActive);

        if (entityId.HasValue)
            query = query.Where(u => u.EntityId == entityId.Value);

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.FullNameAr)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.FullNameAr,
                u.Email,
                u.EntityId,
                EntityNameAr = u.Entity != null ? u.Entity.NameAr : null
            })
            .ToListAsync();

        var userInfos = new List<UserInfo>();
        foreach (var u in users)
        {
            var appUser = await _userManager.FindByIdAsync(u.Id.ToString());
            var roles = appUser != null ? await _userManager.GetRolesAsync(appUser) : [];

            userInfos.Add(new UserInfo
            {
                Id = u.Id,
                UserName = u.UserName!,
                FullNameAr = u.FullNameAr,
                Email = u.Email!,
                Role = roles.FirstOrDefault() ?? string.Empty,
                EntityId = u.EntityId,
                EntityNameAr = u.EntityNameAr
            });
        }

        return ApiResponse<PaginatedResponse<UserInfo>>.Ok(new PaginatedResponse<UserInfo>
        {
            Items = userInfos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ApiResponse<UserInfo>> GetUserByIdAsync(int id, int callerEntityId, string callerRole)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null || !user.IsActive)
            return ApiResponse<UserInfo>.Fail("المستخدم غير موجود");

        // S5 — entity admins may only see users in their own entity. Reveal nothing to lower roles.
        if (callerRole == IndicatorsManagement.Contracts.Constants.Roles.EntityAdmin
            && user.EntityId != callerEntityId)
        {
            return ApiResponse<UserInfo>.Fail("المستخدم غير موجود");
        }

        var roles = await _userManager.GetRolesAsync(user);
        string? entityNameAr = null;
        if (user.EntityId.HasValue)
        {
            entityNameAr = await _db.Entities
                .Where(e => e.Id == user.EntityId.Value)
                .Select(e => e.NameAr)
                .FirstOrDefaultAsync();
        }

        return ApiResponse<UserInfo>.Ok(new UserInfo
        {
            Id = user.Id,
            UserName = user.UserName!,
            FullNameAr = user.FullNameAr,
            Email = user.Email!,
            Role = roles.FirstOrDefault() ?? string.Empty,
            EntityId = user.EntityId,
            EntityNameAr = entityNameAr
        });
    }

    public async Task<ApiResponse<UserInfo>> CreateUserAsync(CreateUserRequest request, int creatorUserId)
    {
        // Validate role is a known role name
        if (!IndicatorsManagement.Contracts.Constants.Roles.All.Contains(request.Role))
            return ApiResponse<UserInfo>.Fail("الدور المحدد غير صالح");

        // Prevent privilege escalation: creator cannot grant a role higher than their own
        var creator = await _userManager.FindByIdAsync(creatorUserId.ToString());
        if (creator is not null)
        {
            var creatorRoles = await _userManager.GetRolesAsync(creator);
            var creatorRole = creatorRoles.FirstOrDefault() ?? string.Empty;
            if (!CanAssignRole(creatorRole, request.Role))
                return ApiResponse<UserInfo>.Fail("لا تملك صلاحية إسناد هذا الدور");
        }

        // Validate entity exists if provided
        if (request.EntityId.HasValue)
        {
            var entityExists = await _db.Entities.AnyAsync(e => e.Id == request.EntityId.Value);
            if (!entityExists)
                return ApiResponse<UserInfo>.Fail("الجهة المحددة غير موجودة");
        }

        // Non–Super_Admin roles other than Ministry_Admin must belong to an entity
        var roleNeedsEntity = request.Role != IndicatorsManagement.Contracts.Constants.Roles.SuperAdmin
                              && request.Role != IndicatorsManagement.Contracts.Constants.Roles.MinistryAdmin;
        if (roleNeedsEntity && !request.EntityId.HasValue)
            return ApiResponse<UserInfo>.Fail("يجب تحديد الجهة لهذا الدور");

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            FullNameAr = request.FullNameAr,
            Phone = request.Phone,
            EntityId = request.EntityId,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ApiResponse<UserInfo>.Fail("فشل إنشاء المستخدم", errors);
        }

        await _userManager.AddToRoleAsync(user, request.Role);
        // S6 — single-role guarantee. AddToRoleAsync is a no-op if already in role; for CreateUser
        // the user has no roles yet, so this is the whole story.

        await _audit.LogAsync(creatorUserId, "User", user.Id, "Create_User",
            newValues: JsonSerializer.Serialize(new { request.UserName, request.Email, request.Role, request.EntityId }));

        return ApiResponse<UserInfo>.Ok(new UserInfo
        {
            Id = user.Id,
            UserName = user.UserName,
            FullNameAr = user.FullNameAr,
            Email = user.Email,
            Role = request.Role,
            EntityId = user.EntityId
        }, "تم إنشاء المستخدم بنجاح");
    }

    private static bool CanAssignRole(string actorRole, string targetRole)
    {
        // Super_Admin: any role. Ministry_Admin: anything except Super_Admin.
        // Entity_Admin: only entity-scoped operational roles.
        return actorRole switch
        {
            IndicatorsManagement.Contracts.Constants.Roles.SuperAdmin => true,
            IndicatorsManagement.Contracts.Constants.Roles.MinistryAdmin =>
                targetRole != IndicatorsManagement.Contracts.Constants.Roles.SuperAdmin,
            IndicatorsManagement.Contracts.Constants.Roles.EntityAdmin =>
                targetRole is IndicatorsManagement.Contracts.Constants.Roles.DataEntryUser
                    or IndicatorsManagement.Contracts.Constants.Roles.Reviewer
                    or IndicatorsManagement.Contracts.Constants.Roles.Auditor
                    or IndicatorsManagement.Contracts.Constants.Roles.Viewer,
            _ => false,
        };
    }

    public async Task<ApiResponse<UserInfo>> UpdateUserAsync(int id, UpdateUserRequest request, int updaterUserId, int callerEntityId, string callerRole)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return ApiResponse<UserInfo>.Fail("المستخدم غير موجود");

        // S5 — entity admins may only touch users in their own entity.
        if (callerRole == IndicatorsManagement.Contracts.Constants.Roles.EntityAdmin
            && user.EntityId != callerEntityId)
        {
            return ApiResponse<UserInfo>.Fail("المستخدم غير موجود");
        }

        // Validate the target role is a known role.
        if (!IndicatorsManagement.Contracts.Constants.Roles.All.Contains(request.Role))
            return ApiResponse<UserInfo>.Fail("الدور المحدد غير صالح");

        // Prevent privilege escalation on update as well.
        if (!CanAssignRole(callerRole, request.Role))
            return ApiResponse<UserInfo>.Fail("لا تملك صلاحية إسناد هذا الدور");

        var oldValues = JsonSerializer.Serialize(new
        {
            user.FullNameAr, user.Phone, user.Email, user.EntityId
        });

        user.FullNameAr = request.FullNameAr;
        user.Phone = request.Phone;
        user.Email = request.Email;
        user.EntityId = request.EntityId;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ApiResponse<UserInfo>.Fail("فشل تحديث المستخدم", errors);
        }

        // S6 — strip every existing role before adding the new one so a user is never
        // multi-role. This fixes the "only first role is claimed" ambiguity in JWTs.
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, request.Role);

        await _audit.LogAsync(updaterUserId, "User", user.Id, "Update_User",
            oldValues: oldValues,
            newValues: JsonSerializer.Serialize(new { request.FullNameAr, request.Phone, request.Email, request.Role, request.EntityId }));

        return ApiResponse<UserInfo>.Ok(new UserInfo
        {
            Id = user.Id,
            UserName = user.UserName!,
            FullNameAr = user.FullNameAr,
            Email = user.Email!,
            Role = request.Role,
            EntityId = user.EntityId
        }, "تم تحديث المستخدم بنجاح");
    }

    public async Task<ApiResponse> DeactivateUserAsync(int id, int deactivatorUserId)
    {
        // Prevent self-deactivation — an admin must never lock themselves out.
        if (id == deactivatorUserId)
            return ApiResponse.Fail("لا يمكنك تعطيل حسابك الشخصي");

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return ApiResponse.Fail("المستخدم غير موجود");

        if (!user.IsActive)
            return ApiResponse.Fail("الحساب معطّل مسبقاً");

        // Prevent removing the last active Super_Admin so the system can never be locked out.
        var userRoles = await _userManager.GetRolesAsync(user);
        if (userRoles.Contains(IndicatorsManagement.Contracts.Constants.Roles.SuperAdmin))
        {
            var otherActiveSuperAdmins = await _db.Users
                .Where(u => u.IsActive && u.Id != id)
                .Join(_db.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur.RoleId })
                .Join(_db.Roles, x => x.RoleId, r => r.Id, (x, r) => r.Name)
                .Where(name => name == IndicatorsManagement.Contracts.Constants.Roles.SuperAdmin)
                .CountAsync();
            if (otherActiveSuperAdmins == 0)
                return ApiResponse.Fail("لا يمكن تعطيل آخر مدير نظام نشط");
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Revoke all active sessions
        var sessions = await _db.UserSessions.Where(s => s.UserId == id).ToListAsync();
        _db.UserSessions.RemoveRange(sessions);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(deactivatorUserId, "User", user.Id, "Deactivate_User");

        return ApiResponse.Ok("تم تعطيل حساب المستخدم بنجاح");
    }
}
