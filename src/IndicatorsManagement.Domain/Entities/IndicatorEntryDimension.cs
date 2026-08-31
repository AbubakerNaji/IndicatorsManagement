using IndicatorsManagement.Domain.Common;

namespace IndicatorsManagement.Domain.Entities;

public class IndicatorEntryDimension : BaseEntity
{
    public int IndicatorEntryId { get; set; }
    public int DimensionId { get; set; }
    public int? DimensionValueId { get; set; }
    public decimal? ValueNumeric { get; set; }

    public virtual IndicatorEntry IndicatorEntry { get; set; } = null!;
    public virtual Dimension Dimension { get; set; } = null!;
    public virtual DimensionValue? DimensionValue { get; set; }
}
