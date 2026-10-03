using EventArch.Application.Validation;
using FluentValidation;

namespace EventArch.Application.Accounts.Withdraw;

internal sealed class WithdrawValidator : AbstractValidator<WithdrawCommand>
{
    public WithdrawValidator()
    {
        RuleFor(command => command.AccountId).NotEmpty();
        RuleFor(command => command.Amount).MustBeValidAmount();
    }
}
