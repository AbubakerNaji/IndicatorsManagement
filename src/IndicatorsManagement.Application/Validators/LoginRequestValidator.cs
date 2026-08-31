using FluentValidation;
using IndicatorsManagement.Contracts.Requests;

namespace IndicatorsManagement.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UserNameOrEmail)
            .NotEmpty().WithMessage("اسم المستخدم أو البريد الإلكتروني مطلوب");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة");
    }
}
