using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Contracts.Requests;

public class CreateIndicatorRequest
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
    public bool RequiresAttachment { get; set; }
    public bool RequiresReview { get; set; } = true;
    public List<CreateDimensionRequest>? Dimensions { get; set; }
}

public class UpdateIndicatorRequest
{
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string DefinitionAr { get; set; } = string.Empty;
    public string CalculationMethodAr { get; set; } = string.Empty;
    public string UnitAr { get; set; } = string.Empty;
    public string DataSourceAr { get; set; } = string.Empty;
    public string? ObjectiveAr { get; set; }
    public PublicationFrequency PublicationFrequency { get; set; }
    public bool RequiresAttachment { get; set; }
    public bool RequiresReview { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class CreateDimensionRequest
{
    public string DimensionNameAr { get; set; } = string.Empty;
    public DimensionType DimensionType { get; set; }
    public bool IsMandatory { get; set; } = true;
    public int DisplayOrder { get; set; }
    public List<CreateDimensionValueRequest>? Values { get; set; }
}

public class CreateDimensionValueRequest
{
    public string ValueAr { get; set; } = string.Empty;
    public string? ValueEn { get; set; }
    public int DisplayOrder { get; set; }
}
