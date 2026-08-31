namespace IndicatorsManagement.Contracts.Responses;

public class DraftRecoveryResponse
{
    public int Id { get; set; }
    public int IndicatorId { get; set; }
    public string IndicatorNameAr { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public int ReportingPeriodId { get; set; }
    public string PeriodDisplayNameAr { get; set; } = string.Empty;
    public string DraftDataJson { get; set; } = string.Empty;
    public DateTime LastSavedAt { get; set; }
}
