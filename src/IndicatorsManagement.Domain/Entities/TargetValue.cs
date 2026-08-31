using IndicatorsManagement.Domain.Common;

namespace IndicatorsManagement.Domain.Entities;

public class TargetValue : BaseEntity
{
    public int IndicatorId { get; set; }
    public int Year { get; set; }
    public int? EntityId { get; set; }
    public int? DimensionValueId { get; set; }
    public decimal Value { get; set; }
    public int CreatedBy { get; set; }

    public virtual Indicator Indicator { get; set; } = null!;
    public virtual Entity? Entity { get; set; }
    public virtual DimensionValue? DimensionValue { get; set; }
    public virtual ApplicationUser Creator { get; set; } = null!;
}
