using EventArch.Application.Abstractions;
using EventArch.Domain.Accounts;
using Microsoft.EntityFrameworkCore;

namespace EventArch.Infrastructure.Persistence;

internal sealed class AccountRepository(EventArchDbContext dbContext) : IAccountRepository
{
    public Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken)
    {
        return dbContext.Accounts.FirstOrDefaultAsync(account => account.Id == accountId, cancellationToken);
    }

    public void Add(Account account)
    {
        dbContext.Accounts.Add(account);
    }
}
