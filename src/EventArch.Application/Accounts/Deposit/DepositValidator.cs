using EventArch.Application.Validation;
using FluentValidation;

namespace EventArch.Application.Accounts.Deposit;

internal sealed class DepositValidator : AbstractValidator<DepositCommand>
{
    public DepositValidator()
    {
        RuleFor(command => command.AccountId).NotEmpty();
        RuleFor(command => command.Amount).MustBeValidAmount();
    }
}
