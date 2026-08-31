using IndicatorsManagement.Domain.Common;
using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Domain.Entities;

public class SubmissionObligation : BaseEntity
{
    public int IndicatorAssignmentId { get; set; }
    public int ReportingPeriodId { get; set; }
    public DateOnly DueDate { get; set; }
    public ObligationStatus Status { get; set; } = ObligationStatus.Not_Started;

    public virtual IndicatorAssignment IndicatorAssignment { get; set; } = null!;
    public virtual ReportingPeriod ReportingPeriod { get; set; } = null!;
}
