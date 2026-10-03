using EventArch.Application.Abstractions;
using EventArch.Domain.Accounts;
using EventArch.Domain.Common;

namespace EventArch.Application.Accounts.GetAccount;

internal sealed class GetAccountHandler(IAccountRepository accountRepository)
    : IQueryHandler<GetAccountQuery, AccountResponse>
{
    public async Task<Result<AccountResponse>> HandleAsync(GetAccountQuery query, CancellationToken cancellationToken)
    {
        Account? account = await accountRepository.GetByIdAsync(query.AccountId, cancellationToken);
        if (account is null)
        {
            return AccountErrors.NotFound(query.AccountId);
        }

        return new AccountResponse(
            account.Id,
            account.HolderName,
            account.Balance.Value,
            Money.Currency,
            account.Status.ToString());
    }
}
