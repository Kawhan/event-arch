using EventArch.Application.Abstractions;
using EventArch.Domain.Accounts;
using EventArch.Domain.Common;

namespace EventArch.Application.Accounts.Withdraw;

internal sealed class WithdrawHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<WithdrawCommand>
{
    public async Task<Result> HandleAsync(WithdrawCommand command, CancellationToken cancellationToken)
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

        Result withdrawResult = account.Withdraw(amountResult.Value);
        if (withdrawResult.IsFailure)
        {
            return withdrawResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
