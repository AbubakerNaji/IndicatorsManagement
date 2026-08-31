using FluentValidation;
using IndicatorsManagement.Contracts.Requests;

namespace IndicatorsManagement.Application.Validators;

public class CreateIndicatorRequestValidator : AbstractValidator<CreateIndicatorRequest>
{
    public CreateIndicatorRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("رمز المؤشر مطلوب")
            .MaximumLength(50).WithMessage("رمز المؤشر يجب ألا يتجاوز 50 حرفاً");
        RuleFor(x => x.NameAr).NotEmpty().WithMessage("اسم المؤشر مطلوب");
        RuleFor(x => x.DefinitionAr).NotEmpty().WithMessage("تعريف المؤشر مطلوب");
        RuleFor(x => x.CalculationMethodAr).NotEmpty().WithMessage("آلية الاحتساب مطلوبة");
        RuleFor(x => x.UnitAr).NotEmpty().WithMessage("وحدة القياس مطلوبة");
        RuleFor(x => x.DataSourceAr).NotEmpty().WithMessage("مصدر البيانات مطلوب");
        RuleFor(x => x.PublicationFrequency).IsInEnum().WithMessage("دورية النشر غير صالحة");
    }
}

public class UpdateIndicatorRequestValidator : AbstractValidator<UpdateIndicatorRequest>
{
    public UpdateIndicatorRequestValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().WithMessage("اسم المؤشر مطلوب");
        RuleFor(x => x.DefinitionAr).NotEmpty().WithMessage("تعريف المؤشر مطلوب");
        RuleFor(x => x.CalculationMethodAr).NotEmpty().WithMessage("آلية الاحتساب مطلوبة");
        RuleFor(x => x.UnitAr).NotEmpty().WithMessage("وحدة القياس مطلوبة");
        RuleFor(x => x.DataSourceAr).NotEmpty().WithMessage("مصدر البيانات مطلوب");
        RuleFor(x => x.PublicationFrequency).IsInEnum().WithMessage("دورية النشر غير صالحة");
    }
}
