using IndicatorsManagement.Contracts.Responses;

namespace IndicatorsManagement.Application.Services.Interfaces;

public interface IDraftRecoveryService
{
    Task<ApiResponse> SaveDraftAsync(int userId, int indicatorId, int entityId, int reportingPeriodId, string draftDataJson);
    Task<ApiResponse<List<DraftRecoveryResponse>>> GetRecoverableDraftsAsync(int userId);
    Task<ApiResponse> DiscardDraftAsync(int draftId, int userId);
}
