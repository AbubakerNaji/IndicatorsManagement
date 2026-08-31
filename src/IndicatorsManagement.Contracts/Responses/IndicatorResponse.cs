using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Contracts.Responses;

public class IndicatorResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string DefinitionAr { get; set; } = string.Empty;
    public string CalculationMethodAr { get; set; } = string.Empty;
    public string UnitAr { get; set; } = string.Empty;
    public string DataSourceAr { get; set; } = string.Empty;
    public string? ObjectiveAr { get; set; }
    public PublicationFrequency PublicationFrequency { get; set; }
    public bool IsActive { get; set; }
    public bool RequiresAttachment { get; set; }
    public bool RequiresReview { get; set; }
    public List<DimensionResponse> Dimensions { get; set; } = [];
}

public class DimensionResponse
{
    public int Id { get; set; }
    public string DimensionNameAr { get; set; } = string.Empty;
    public DimensionType DimensionType { get; set; }
    public bool IsMandatory { get; set; }
    public int DisplayOrder { get; set; }
    public List<DimensionValueResponse> Values { get; set; } = [];
}

public class DimensionValueResponse
{
    public int Id { get; set; }
    public string ValueAr { get; set; } = string.Empty;
    public string? ValueEn { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}
