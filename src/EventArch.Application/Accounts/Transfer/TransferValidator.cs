using EventArch.Application.Validation;
using FluentValidation;

namespace EventArch.Application.Accounts.Transfer;

internal sealed class TransferValidator : AbstractValidator<TransferCommand>
{
    public TransferValidator()
    {
        RuleFor(command => command.SourceAccountId).NotEmpty();

        RuleFor(command => command.DestinationAccountId)
            .NotEmpty()
            .NotEqual(command => command.SourceAccountId)
            .WithMessage("Destination account must be different from the source account.");

        RuleFor(command => command.Amount).MustBeValidAmount();
    }
}
