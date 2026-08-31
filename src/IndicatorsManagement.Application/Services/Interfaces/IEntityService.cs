using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Contracts.Responses;

namespace IndicatorsManagement.Application.Services.Interfaces;

public interface IEntityService
{
    Task<ApiResponse<List<EntityResponse>>> GetEntitiesAsync();
    Task<ApiResponse<EntityResponse>> GetEntityByIdAsync(int id);
    Task<ApiResponse<EntityResponse>> CreateEntityAsync(CreateEntityRequest request, int creatorUserId);
    Task<ApiResponse<EntityResponse>> UpdateEntityAsync(int id, UpdateEntityRequest request, int updaterUserId);
    Task<ApiResponse> DeactivateEntityAsync(int id, int deactivatorUserId);
}
