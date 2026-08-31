using IndicatorsManagement.Contracts.Responses;

namespace IndicatorsManagement.Application.Services.Interfaces;

public interface IDashboardService
{
    Task<ApiResponse<MinistryDashboardResponse>> GetMinistryDashboardAsync();
    // S5 — caller's entity and role decide whether they may read this entity's dashboard.
    Task<ApiResponse<EntityDashboardResponse>> GetEntityDashboardAsync(int entityId, int userEntityId, string userRole);
    Task<ApiResponse<List<TaskItemResponse>>> GetUserTasksAsync(int userId, int entityId);
}
