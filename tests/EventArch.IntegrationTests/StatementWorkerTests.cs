using EventArch.Contracts.Accounts;
using EventArch.IntegrationTests.Infrastructure;
using EventArch.Statement.Worker.Persistence;
using EventArch.Statement.Worker.Statements;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;

namespace EventArch.IntegrationTests;

/// <summary>
/// The statement worker: events become statement lines, exactly once per event.
/// </summary>
[Collection(ApiCollection.Name)]
public class StatementWorkerTests(EventArchApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task DepositThroughApi_BecomesCreditLineInStatement()
    {
        Guid accountId = await client.OpenAccountAsync();

        await client.DepositAsync(accountId, 75m);

        List<StatementEntry> entries = await Eventually.GetAsync(
            async () =>
            {
                List<StatementEntry> found = await GetEntriesAsync(accountId);
                return found.Count > 0 ? found : null;
            },
            "The deposit never reached the statement.");

        StatementEntry entry = Assert.Single(entries);
        Assert.Equal(EntryKind.Credit, entry.Kind);
        Assert.Equal(75m, entry.Amount);
        Assert.Equal(75m, entry.BalanceAfter);
    }

    [Fact]
    public async Task SameEventDeliveredTwice_IsRecordedOnce()
    {
        Guid accountId = Guid.NewGuid();
        var duplicated = NewDeposit(accountId, amount: 10m, balanceAfter: 10m);

        // Simulates the Outbox publishing the same event again (at-least-once delivery).
        IBus bus = factory.WorkerServices.GetRequiredService<IBus>();
        await bus.Publish(duplicated);
        await bus.Publish(duplicated);

        // The worker handles messages in order, so once this marker is recorded
        // both copies of the duplicated event have already been processed.
        var marker = NewDeposit(accountId, amount: 5m, balanceAfter: 15m);
        await bus.Publish(marker);

        List<StatementEntry> entries = await Eventually.GetAsync(
            async () =>
            {
                List<StatementEntry> found = await GetEntriesAsync(accountId);
                return found.Any(entry => entry.EventId == marker.EventId) ? found : null;
            },
            "The marker event never reached the statement.");

        Assert.Single(entries, entry => entry.EventId == duplicated.EventId);
        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public async Task SameEventRecordedConcurrently_StoresOneLine()
    {
        Guid eventId = Guid.NewGuid();
        Guid accountId = Guid.NewGuid();

        // Ten parallel recordings, each with its own scope and DbContext, as several worker
        // instances would do. Many attempts make a real race between check and insert likely.
        Task[] recordings = Enumerable.Range(0, 10).Select(async _ =>
        {
            await using AsyncServiceScope scope = factory.WorkerServices.CreateAsyncScope();
            var recorder = scope.ServiceProvider.GetRequiredService<StatementRecorder>();

            var entry = StatementEntry.Create(
                eventId, accountId, DateTime.UtcNow, EntryKind.Credit, amount: 1m, balanceAfter: 1m, transferId: null);

            await recorder.RecordAsync(entry, CancellationToken.None);
        }).ToArray();

        // No recording may fail: losing the race is an expected, handled situation.
        await Task.WhenAll(recordings);

        Assert.Single(await GetEntriesAsync(accountId));
    }

    private static MoneyDepositedIntegrationEvent NewDeposit(Guid accountId, decimal amount, decimal balanceAfter)
    {
        return new MoneyDepositedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, accountId, amount, balanceAfter, TransferId: null);
    }

    private async Task<List<StatementEntry>> GetEntriesAsync(Guid accountId)
    {
        await using AsyncServiceScope scope = factory.WorkerServices.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StatementDbContext>();

        return await dbContext.StatementEntries
            .AsNoTracking()
            .Where(entry => entry.AccountId == accountId)
            .ToListAsync();
    }
}
