namespace IndicatorsManagement.Contracts.Responses;

public class SearchResultsResponse
{
    public List<SearchResultItem> Items { get; set; } = [];
    public int TotalCount { get; set; }
}

public class SearchResultItem
{
    public string Type { get; set; } = string.Empty; // Indicator, Entity, Entry
    public int Id { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string? SubtitleAr { get; set; }
    public string? Status { get; set; }
}
