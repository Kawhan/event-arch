using EventArch.Application.Abstractions;
using EventArch.Domain.Accounts;
using EventArch.Domain.Common;

namespace EventArch.Application.Accounts.FreezeAccount;

internal sealed class FreezeAccountHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<FreezeAccountCommand>
{
    public async Task<Result> HandleAsync(FreezeAccountCommand command, CancellationToken cancellationToken)
    {
        Account? account = await accountRepository.GetByIdAsync(command.AccountId, cancellationToken);
        if (account is null)
        {
            return AccountErrors.NotFound(command.AccountId);
        }

        Result freezeResult = account.Freeze();
        if (freezeResult.IsFailure)
        {
            return freezeResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
