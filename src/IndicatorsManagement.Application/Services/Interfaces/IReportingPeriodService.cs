using IndicatorsManagement.Contracts.Requests;
using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Application.Services.Interfaces;

public interface IReportingPeriodService
{
    Task<ApiResponse<List<ReportingPeriodResponse>>> GetPeriodsAsync(PeriodType? periodType = null, int? year = null);
    Task<ApiResponse<int>> GeneratePeriodsAsync(GenerateReportingPeriodsRequest request, int creatorUserId);
}
