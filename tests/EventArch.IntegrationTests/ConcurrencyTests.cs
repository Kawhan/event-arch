using EventArch.Application.Abstractions;
using EventArch.Domain.Accounts;
using EventArch.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EventArch.IntegrationTests;

/// <summary>
/// Optimistic concurrency (PostgreSQL xmin) must stop two requests from
/// spending the same balance.
/// </summary>
[Collection(ApiCollection.Name)]
public class ConcurrencyTests(EventArchApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task TwoWithdrawalsFromTheSameLoadedState_SecondSaveIsRejected()
    {
        Guid accountId = await client.OpenAccountAsync();
        await client.DepositAsync(accountId, 100m);
        Money amount = Money.Create(80m).Value;

        // Two independent scopes simulate two simultaneous requests: both read
        // a balance of 100, so both withdrawals look valid in memory.
        await using AsyncServiceScope firstRequest = factory.Services.CreateAsyncScope();
        await using AsyncServiceScope secondRequest = factory.Services.CreateAsyncScope();

        Account firstCopy = (await Repository(firstRequest).GetByIdAsync(accountId, CancellationToken.None))!;
        Account secondCopy = (await Repository(secondRequest).GetByIdAsync(accountId, CancellationToken.None))!;

        Assert.True(firstCopy.Withdraw(amount).IsSuccess);
        Assert.True(secondCopy.Withdraw(amount).IsSuccess);

        await UnitOfWork(firstRequest).SaveChangesAsync(CancellationToken.None);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => UnitOfWork(secondRequest).SaveChangesAsync(CancellationToken.None));

        // Only the first withdrawal happened; the balance never went negative.
        Assert.Equal(20m, await client.GetBalanceAsync(accountId));
    }

    private static IAccountRepository Repository(AsyncServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IAccountRepository>();

    private static IUnitOfWork UnitOfWork(AsyncServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
}
