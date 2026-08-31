using IndicatorsManagement.Domain.Common;

namespace IndicatorsManagement.Domain.Entities;

public class DimensionValue : BaseEntity
{
    public int DimensionId { get; set; }
    public string ValueAr { get; set; } = string.Empty;
    public string? ValueEn { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Dimension Dimension { get; set; } = null!;
}
