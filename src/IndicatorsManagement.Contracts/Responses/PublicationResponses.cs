namespace IndicatorsManagement.Contracts.Responses;

public class PublicationHistoryResponse
{
    public int Id { get; set; }
    public int IndicatorEntryId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string PerformedByName { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public string? Reason { get; set; }
}

public class PublicationStatisticsResponse
{
    public int TotalPublished { get; set; }
    public int TotalUnpublished { get; set; }
    public int TotalFinalApproved { get; set; }
    public decimal PublicationRate { get; set; }
    public DateTime? LatestPublicationDate { get; set; }
}
