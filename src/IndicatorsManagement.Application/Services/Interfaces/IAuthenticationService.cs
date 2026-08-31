using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Contracts.Responses;

namespace IndicatorsManagement.Application.Services.Interfaces;

public interface IAuthenticationService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent);
    Task<ApiResponse> LogoutAsync(int userId, string sessionToken);
    Task<ApiResponse> AdminResetPasswordAsync(AdminResetPasswordRequest request, int adminUserId);
}
