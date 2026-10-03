using EventArch.Application.Abstractions;
using EventArch.Domain.Accounts;
using EventArch.Domain.Common;

namespace EventArch.Application.Accounts.CloseAccount;

internal sealed class CloseAccountHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CloseAccountCommand>
{
    public async Task<Result> HandleAsync(CloseAccountCommand command, CancellationToken cancellationToken)
    {
        Account? account = await accountRepository.GetByIdAsync(command.AccountId, cancellationToken);
        if (account is null)
        {
            return AccountErrors.NotFound(command.AccountId);
        }

        Result closeResult = account.Close();
        if (closeResult.IsFailure)
        {
            return closeResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
