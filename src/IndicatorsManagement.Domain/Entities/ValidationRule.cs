using IndicatorsManagement.Domain.Common;
using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Domain.Entities;

public class ValidationRule : BaseEntity
{
    public int IndicatorId { get; set; }
    public ValidationRuleType RuleType { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public bool IsMandatoryNotes { get; set; }
    public bool IsMandatoryAttachment { get; set; }

    public virtual Indicator Indicator { get; set; } = null!;
}
