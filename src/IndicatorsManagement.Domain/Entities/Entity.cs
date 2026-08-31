using IndicatorsManagement.Domain.Common;
using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Domain.Entities;

public class Entity : BaseEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public EntityType Type { get; set; }
    public int? ParentEntityId { get; set; }
    public string Status { get; set; } = "active";

    public virtual Entity? ParentEntity { get; set; }
    public virtual ICollection<Entity> ChildEntities { get; set; } = [];
    public virtual ICollection<IndicatorAssignment> IndicatorAssignments { get; set; } = [];
}
