using EventArch.Application.Abstractions;
using EventArch.Domain.Accounts;
using EventArch.Domain.Common;

namespace EventArch.Application.Accounts.Transfer;

internal sealed class TransferHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<TransferCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(TransferCommand command, CancellationToken cancellationToken)
    {
        Result<Money> amountResult = Money.Create(command.Amount);
        if (amountResult.IsFailure)
        {
            return amountResult.Error!;
        }

        Account? source = await accountRepository.GetByIdAsync(command.SourceAccountId, cancellationToken);
        if (source is null)
        {
            return AccountErrors.NotFound(command.SourceAccountId);
        }

        Account? destination = await accountRepository.GetByIdAsync(command.DestinationAccountId, cancellationToken);
        if (destination is null)
        {
            return AccountErrors.NotFound(command.DestinationAccountId);
        }

        Result<Guid> transferResult = TransferService.Transfer(source, destination, amountResult.Value);
        if (transferResult.IsFailure)
        {
            return transferResult;
        }

        // Both accounts and all transfer events are saved in one transaction.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return transferResult;
    }
}
