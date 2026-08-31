using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Contracts.Requests;

public class CreateAssignmentRequest
{
    public int IndicatorId { get; set; }
    public int EntityId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public PeriodType ReportingFrequency { get; set; }
}

public class UpdateAssignmentRequest
{
    public DateOnly? EndDate { get; set; }
    public PeriodType ReportingFrequency { get; set; }
    public bool IsActive { get; set; } = true;
}
