using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Contracts.Responses;

namespace IndicatorsManagement.Application.Services.Interfaces;

public interface IIndicatorService
{
    Task<ApiResponse<PaginatedResponse<IndicatorResponse>>> GetIndicatorsAsync(bool? isActive = null, int page = 1, int pageSize = 20);
    Task<ApiResponse<IndicatorResponse>> GetIndicatorByIdAsync(int id);
    Task<ApiResponse<IndicatorResponse>> CreateIndicatorAsync(CreateIndicatorRequest request, int creatorUserId);
    Task<ApiResponse<IndicatorResponse>> UpdateIndicatorAsync(int id, UpdateIndicatorRequest request, int updaterUserId);
    Task<ApiResponse> DeleteIndicatorAsync(int id, int deleterUserId);

    // Dimension management
    Task<ApiResponse<DimensionResponse>> AddDimensionAsync(int indicatorId, CreateDimensionRequest request, int userId);
    Task<ApiResponse<DimensionResponse>> UpdateDimensionAsync(int dimensionId, CreateDimensionRequest request, int userId);
    Task<ApiResponse> DeleteDimensionAsync(int dimensionId, int userId);
}
