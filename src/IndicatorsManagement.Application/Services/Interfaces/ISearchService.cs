using IndicatorsManagement.Contracts.Responses;

namespace IndicatorsManagement.Application.Services.Interfaces;

public interface ISearchService
{
    Task<ApiResponse<SearchResultsResponse>> SearchAsync(string query, int? entityId, int page = 1, int pageSize = 10);
}
