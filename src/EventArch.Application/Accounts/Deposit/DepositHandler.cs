using EventArch.Application.Abstractions;
using EventArch.Domain.Accounts;
using EventArch.Domain.Common;

namespace EventArch.Application.Accounts.Deposit;

internal sealed class DepositHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<DepositCommand>
{
    public async Task<Result> HandleAsync(DepositCommand command, CancellationToken cancellationToken)
    {
        Result<Money> amountResult = Money.Create(command.Amount);
        if (amountResult.IsFailure)
        {
            return amountResult.Error!;
        }

        Account? account = await accountRepository.GetByIdAsync(command.AccountId, cancellationToken);
        if (account is null)
        {
            return AccountErrors.NotFound(command.AccountId);
        }

        Result depositResult = account.Deposit(amountResult.Value);
        if (depositResult.IsFailure)
        {
            return depositResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
