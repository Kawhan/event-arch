using EventArch.Domain.Accounts;

namespace EventArch.Application.Abstractions;

/// <summary>
/// Loads and tracks <see cref="Account"/> aggregates.
/// Changes are only persisted by <see cref="IUnitOfWork.SaveChangesAsync"/>.
/// </summary>
public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken);

    void Add(Account account);
}
