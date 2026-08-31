using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Services;

public class SearchService : ISearchService
{
    private readonly IndicatorsDbContext _db;

    public SearchService(IndicatorsDbContext db) => _db = db;

    public async Task<ApiResponse<SearchResultsResponse>> SearchAsync(string query, int? entityId, int page = 1, int pageSize = 10)
    {
        var results = new List<SearchResultItem>();

        // Search indicators
        var indicators = await _db.Indicators.AsNoTracking()
            .Where(i => i.NameAr.Contains(query) || i.Code.Contains(query) || i.DefinitionAr.Contains(query))
            .Take(pageSize)
            .Select(i => new SearchResultItem
            {
                Type = "Indicator", Id = i.Id, TitleAr = i.NameAr,
                SubtitleAr = i.Code, Status = i.IsActive ? "فعّال" : "معطّل"
            }).ToListAsync();
        results.AddRange(indicators);

        // Search entities
        var entities = await _db.Entities.AsNoTracking()
            .Where(e => e.NameAr.Contains(query))
            .Take(pageSize)
            .Select(e => new SearchResultItem
            {
                Type = "Entity", Id = e.Id, TitleAr = e.NameAr,
                SubtitleAr = e.Type.ToString(), Status = e.Status
            }).ToListAsync();
        results.AddRange(entities);

        // Search entries (scoped by entity if provided)
        var entryQuery = _db.IndicatorEntries.AsNoTracking()
            .Where(e => !e.IsDeleted)
            .Include(e => e.Indicator)
            .Include(e => e.ReportingPeriod)
            .Where(e => e.Indicator.NameAr.Contains(query) || e.Indicator.Code.Contains(query)
                || (e.Notes != null && e.Notes.Contains(query)));

        if (entityId.HasValue)
            entryQuery = entryQuery.Where(e => e.EntityId == entityId.Value);

        var entries = await entryQuery.Take(pageSize)
            .Select(e => new SearchResultItem
            {
                Type = "Entry", Id = e.Id, TitleAr = e.Indicator.NameAr,
                SubtitleAr = e.ReportingPeriod.DisplayNameAr, Status = e.WorkflowState.ToString()
            }).ToListAsync();
        results.AddRange(entries);

        return ApiResponse<SearchResultsResponse>.Ok(new SearchResultsResponse
        {
            Items = results.Take(pageSize).ToList(),
            TotalCount = results.Count
        });
    }
}
