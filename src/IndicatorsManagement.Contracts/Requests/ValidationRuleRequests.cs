using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Contracts.Responses;

public class CreateValidationRuleRequest
{
    public int IndicatorId { get; set; }
    public ValidationRuleType RuleType { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public bool IsMandatoryNotes { get; set; }
    public bool IsMandatoryAttachment { get; set; }
}

public class ValidationRuleResponse
{
    public int Id { get; set; }
    public int IndicatorId { get; set; }
    public ValidationRuleType RuleType { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public bool IsMandatoryNotes { get; set; }
    public bool IsMandatoryAttachment { get; set; }
}
