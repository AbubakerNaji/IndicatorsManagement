using IndicatorsManagement.Contracts.Responses;

namespace IndicatorsManagement.Application.Services.Interfaces;

public interface IPublicationService
{
    Task<ApiResponse> PublishEntryAsync(int entryId, int userId, string? reason);
    Task<ApiResponse> UnpublishEntryAsync(int entryId, int userId, string? reason);
    Task<ApiResponse<int>> BulkPublishAsync(List<int> entryIds, int userId, string? reason);
    Task<ApiResponse<int>> BulkUnpublishAsync(List<int> entryIds, int userId, string? reason);
    Task<ApiResponse<List<PublicationHistoryResponse>>> GetPublicationHistoryAsync(int entryId);
    Task<ApiResponse<PublicationStatisticsResponse>> GetPublicationStatisticsAsync();
}
