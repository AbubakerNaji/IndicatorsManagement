using FluentValidation;
using IndicatorsManagement.Contracts.Requests;

namespace IndicatorsManagement.Application.Validators;

public class CreateEntityRequestValidator : AbstractValidator<CreateEntityRequest>
{
    public CreateEntityRequestValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().WithMessage("اسم الجهة مطلوب");
        RuleFor(x => x.Type).IsInEnum().WithMessage("نوع الجهة غير صالح");
    }
}

public class UpdateEntityRequestValidator : AbstractValidator<UpdateEntityRequest>
{
    public UpdateEntityRequestValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().WithMessage("اسم الجهة مطلوب");
        RuleFor(x => x.Type).IsInEnum().WithMessage("نوع الجهة غير صالح");
        RuleFor(x => x.Status).NotEmpty().WithMessage("حالة الجهة مطلوبة")
            .Must(s => s is "active" or "inactive").WithMessage("حالة الجهة يجب أن تكون active أو inactive");
    }
}
