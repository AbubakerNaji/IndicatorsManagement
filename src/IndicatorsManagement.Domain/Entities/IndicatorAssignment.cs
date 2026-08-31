using IndicatorsManagement.Domain.Common;
using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Domain.Entities;

public class IndicatorAssignment : BaseEntity
{
    public int IndicatorId { get; set; }
    public int EntityId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public PeriodType ReportingFrequency { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Indicator Indicator { get; set; } = null!;
    public virtual Entity Entity { get; set; } = null!;
    public virtual ICollection<SubmissionObligation> Obligations { get; set; } = [];
}
