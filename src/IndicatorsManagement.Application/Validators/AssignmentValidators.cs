using FluentValidation;
using IndicatorsManagement.Contracts.Requests;

namespace IndicatorsManagement.Application.Validators;

public class CreateAssignmentRequestValidator : AbstractValidator<CreateAssignmentRequest>
{
    public CreateAssignmentRequestValidator()
    {
        RuleFor(x => x.IndicatorId).GreaterThan(0).WithMessage("معرف المؤشر غير صالح");
        RuleFor(x => x.EntityId).GreaterThan(0).WithMessage("معرف الجهة غير صالح");
        RuleFor(x => x.ReportingFrequency).IsInEnum().WithMessage("دورية الإبلاغ غير صالحة");
    }
}
