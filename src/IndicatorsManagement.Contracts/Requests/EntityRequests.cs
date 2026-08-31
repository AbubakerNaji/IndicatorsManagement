using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Contracts.Requests;

public class CreateEntityRequest
{
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public EntityType Type { get; set; }
    public int? ParentEntityId { get; set; }
}

public class UpdateEntityRequest
{
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public EntityType Type { get; set; }
    public int? ParentEntityId { get; set; }
    public string Status { get; set; } = "active";
}
