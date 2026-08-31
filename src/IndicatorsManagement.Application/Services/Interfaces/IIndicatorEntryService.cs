using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Contracts.Responses;

namespace IndicatorsManagement.Application.Services.Interfaces;

public interface IIndicatorEntryService
{
    Task<ApiResponse<PaginatedResponse<IndicatorEntryResponse>>> GetEntriesAsync(int? entityId = null, int? indicatorId = null, int? periodId = null, int page = 1, int pageSize = 20, bool publishedOnly = false);
    // S5 — every by-id method requires the caller's entity and role so cross-entity access can be denied.
    Task<ApiResponse<IndicatorEntryResponse>> GetEntryByIdAsync(int id, int userEntityId, string userRole);
    Task<ApiResponse<IndicatorEntryResponse>> CreateEntryAsync(CreateIndicatorEntryRequest request, int userId, int userEntityId);
    Task<ApiResponse<IndicatorEntryResponse>> UpdateEntryAsync(int id, UpdateIndicatorEntryRequest request, int userId, int userEntityId, string userRole);
    Task<ApiResponse> SoftDeleteEntryAsync(int id, int userId, int userEntityId, string userRole);

    // Workflow actions — all authorized against the entry's owning entity.
    Task<ApiResponse<IndicatorEntryResponse>> SubmitEntryAsync(int id, int userId, int userEntityId, string userRole);
    Task<ApiResponse<IndicatorEntryResponse>> ApproveEntityLevelAsync(int id, int userId, int userEntityId, string userRole, string? notes);
    Task<ApiResponse<IndicatorEntryResponse>> ApproveMinistryLevelAsync(int id, int userId, string? notes);
    Task<ApiResponse<IndicatorEntryResponse>> RejectEntryAsync(int id, int userId, int userEntityId, string userRole, string notes);
    Task<ApiResponse<IndicatorEntryResponse>> ReturnEntryAsync(int id, int userId, int userEntityId, string userRole, string notes);
}
