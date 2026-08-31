using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Contracts.Responses;

public class AssignmentResponse
{
    public int Id { get; set; }
    public int IndicatorId { get; set; }
    public string IndicatorNameAr { get; set; } = string.Empty;
    public string IndicatorCode { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string EntityNameAr { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public PeriodType ReportingFrequency { get; set; }
    public bool IsActive { get; set; }
}

public class ObligationResponse
{
    public int Id { get; set; }
    public int IndicatorAssignmentId { get; set; }
    public int ReportingPeriodId { get; set; }
    public string PeriodDisplayNameAr { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public ObligationStatus Status { get; set; }
}
