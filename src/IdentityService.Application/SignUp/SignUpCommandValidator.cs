using FluentValidation;

namespace IdentityService.Application.SignUp;

public sealed class SignUpCommandValidator : AbstractValidator<SignUpCommand>
{
    public SignUpCommandValidator()
    {
        RuleFor(command => command.FullName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 2)
            .WithMessage("Full name must contain at least 2 characters.")
            .MaximumLength(200);

        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(command => command.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(200);

        RuleFor(command => command.ConfirmPassword)
            .Equal(command => command.Password)
            .WithMessage("Confirm password must match password.");

        RuleFor(command => command.AcceptTermsAndPrivacy)
            .Equal(true)
            .WithMessage("Terms and privacy policy must be accepted.");
    }
}
