using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Contracts.Responses;

public class EntityResponse
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public EntityType Type { get; set; }
    public int? ParentEntityId { get; set; }
    public string? ParentEntityNameAr { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<EntityResponse> ChildEntities { get; set; } = [];
}
