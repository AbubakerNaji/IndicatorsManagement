using IndicatorsManagement.Domain.Common;
using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Domain.Entities;

public class Dimension : BaseEntity
{
    public int IndicatorId { get; set; }
    public string DimensionNameAr { get; set; } = string.Empty;
    public DimensionType DimensionType { get; set; }
    public bool IsMandatory { get; set; } = true;
    public int DisplayOrder { get; set; }

    public virtual Indicator Indicator { get; set; } = null!;
    public virtual ICollection<DimensionValue> Values { get; set; } = [];
}
