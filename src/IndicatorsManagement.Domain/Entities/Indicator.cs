using IndicatorsManagement.Domain.Common;
using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Domain.Entities;

public class Indicator : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string DefinitionAr { get; set; } = string.Empty;
    public string CalculationMethodAr { get; set; } = string.Empty;
    public string UnitAr { get; set; } = string.Empty;
    public string DataSourceAr { get; set; } = string.Empty;
    public string? ObjectiveAr { get; set; }
    public PublicationFrequency PublicationFrequency { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresAttachment { get; set; }
    public bool RequiresReview { get; set; } = true;
    public int? CreatedBy { get; set; }

    public virtual ApplicationUser? Creator { get; set; }
    public virtual ICollection<Dimension> Dimensions { get; set; } = [];
    public virtual ICollection<IndicatorAssignment> Assignments { get; set; } = [];
    public virtual ICollection<ValidationRule> ValidationRules { get; set; } = [];
}
