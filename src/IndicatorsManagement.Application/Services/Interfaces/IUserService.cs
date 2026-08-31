using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Contracts.Responses;

namespace IndicatorsManagement.Application.Services.Interfaces;

public interface IUserService
{
    Task<ApiResponse<PaginatedResponse<UserInfo>>> GetUsersAsync(int? entityId, int page = 1, int pageSize = 20);
    // S5 — reading a user by id is scoped: entity-level admins may only see users in their own entity.
    Task<ApiResponse<UserInfo>> GetUserByIdAsync(int id, int callerEntityId, string callerRole);
    Task<ApiResponse<UserInfo>> CreateUserAsync(CreateUserRequest request, int creatorUserId);
    Task<ApiResponse<UserInfo>> UpdateUserAsync(int id, UpdateUserRequest request, int updaterUserId, int callerEntityId, string callerRole);
    Task<ApiResponse> DeactivateUserAsync(int id, int deactivatorUserId);
}
