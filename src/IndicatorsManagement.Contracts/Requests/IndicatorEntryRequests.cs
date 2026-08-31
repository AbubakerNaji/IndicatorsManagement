namespace IndicatorsManagement.Contracts.Requests;

public class CreateIndicatorEntryRequest
{
    public int IndicatorId { get; set; }
    public int ReportingPeriodId { get; set; }
    public decimal? ValueNumeric { get; set; }
    public string? ValueText { get; set; }
    public string? Notes { get; set; }
    public string? Source { get; set; }
    public List<EntryDimensionRequest>? Dimensions { get; set; }
}

public class UpdateIndicatorEntryRequest
{
    public decimal? ValueNumeric { get; set; }
    public string? ValueText { get; set; }
    public string? Notes { get; set; }
    public string? Source { get; set; }
    public List<EntryDimensionRequest>? Dimensions { get; set; }
}

public class EntryDimensionRequest
{
    public int DimensionId { get; set; }
    public int? DimensionValueId { get; set; }
    public decimal? ValueNumeric { get; set; }
}

public class WorkflowActionRequest
{
    public string? Notes { get; set; }
}
