using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Contracts.Responses;

namespace IndicatorsManagement.Application.Services.Interfaces;

public interface IIndicatorAssignmentService
{
    Task<ApiResponse<PaginatedResponse<AssignmentResponse>>> GetAssignmentsAsync(int? indicatorId = null, int? entityId = null, int page = 1, int pageSize = 20);
    Task<ApiResponse<AssignmentResponse>> CreateAssignmentAsync(CreateAssignmentRequest request, int creatorUserId);
    Task<ApiResponse<AssignmentResponse>> UpdateAssignmentAsync(int id, UpdateAssignmentRequest request, int updaterUserId);
    Task<ApiResponse<int>> GenerateObligationsAsync(int assignmentId, int userId);
}
