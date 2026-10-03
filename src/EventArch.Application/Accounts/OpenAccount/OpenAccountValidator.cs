using EventArch.Application.Validation;
using FluentValidation;

namespace EventArch.Application.Accounts.OpenAccount;

internal sealed class OpenAccountValidator : AbstractValidator<OpenAccountCommand>
{
    public OpenAccountValidator()
    {
        RuleFor(command => command.HolderName)
            .NotEmpty()
            .MaximumLength(ValidationRules.HolderNameMaxLength);
    }
}
