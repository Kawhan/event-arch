using EventArch.Application.Abstractions;
using EventArch.Domain.Accounts;
using EventArch.Domain.Common;

namespace EventArch.Application.Accounts.UnfreezeAccount;

internal sealed class UnfreezeAccountHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UnfreezeAccountCommand>
{
    public async Task<Result> HandleAsync(UnfreezeAccountCommand command, CancellationToken cancellationToken)
    {
        Account? account = await accountRepository.GetByIdAsync(command.AccountId, cancellationToken);
        if (account is null)
        {
            return AccountErrors.NotFound(command.AccountId);
        }

        Result unfreezeResult = account.Unfreeze();
        if (unfreezeResult.IsFailure)
        {
            return unfreezeResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
