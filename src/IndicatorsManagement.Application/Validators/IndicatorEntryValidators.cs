using FluentValidation;
using IndicatorsManagement.Contracts.Requests;

namespace IndicatorsManagement.Application.Validators;

public class CreateIndicatorEntryRequestValidator : AbstractValidator<CreateIndicatorEntryRequest>
{
    public CreateIndicatorEntryRequestValidator()
    {
        RuleFor(x => x.IndicatorId).GreaterThan(0).WithMessage("معرف المؤشر غير صالح");
        RuleFor(x => x.ReportingPeriodId).GreaterThan(0).WithMessage("معرف الفترة غير صالح");
    }
}

public class UpdateIndicatorEntryRequestValidator : AbstractValidator<UpdateIndicatorEntryRequest>
{
    public UpdateIndicatorEntryRequestValidator()
    {
        // ValueNumeric or ValueText should be provided
        RuleFor(x => x)
            .Must(x => x.ValueNumeric.HasValue || !string.IsNullOrEmpty(x.ValueText))
            .WithMessage("يجب إدخال قيمة رقمية أو نصية");
    }
}

public class WorkflowActionRequestValidator : AbstractValidator<WorkflowActionRequest>
{
    public WorkflowActionRequestValidator()
    {
        // Notes can be empty for approve, but reject/return handlers enforce it
    }
}
