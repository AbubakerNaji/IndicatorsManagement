using IndicatorsManagement.Contracts.Responses;

namespace IndicatorsManagement.Application.Services.Interfaces;

public interface IValidationRuleService
{
    Task<ApiResponse<List<ValidationRuleResponse>>> GetRulesByIndicatorAsync(int indicatorId);
    Task<ApiResponse<ValidationRuleResponse>> CreateRuleAsync(CreateValidationRuleRequest request, int userId);
    Task<ApiResponse<ValidationRuleResponse>> UpdateRuleAsync(int id, CreateValidationRuleRequest request, int userId);
    Task<ApiResponse> DeleteRuleAsync(int id, int userId);
}
