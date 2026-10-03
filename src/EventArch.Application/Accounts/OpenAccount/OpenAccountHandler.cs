using EventArch.Application.Abstractions;
using EventArch.Domain.Accounts;
using EventArch.Domain.Common;

namespace EventArch.Application.Accounts.OpenAccount;

internal sealed class OpenAccountHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<OpenAccountCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(OpenAccountCommand command, CancellationToken cancellationToken)
    {
        Result<Account> openResult = Account.Open(command.HolderName);
        if (openResult.IsFailure)
        {
            return openResult.Error!;
        }

        Account account = openResult.Value;
        accountRepository.Add(account);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return account.Id;
    }
}
