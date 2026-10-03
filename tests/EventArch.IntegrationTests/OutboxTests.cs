using System.Net.Http.Json;
using EventArch.Infrastructure.Outbox;
using EventArch.Infrastructure.Persistence;
using EventArch.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventArch.IntegrationTests;

/// <summary>
/// The Outbox guarantees: events are stored with the change, never without it,
/// and are eventually published.
/// </summary>
[Collection(ApiCollection.Name)]
public class OutboxTests(EventArchApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Deposit_StoresEventInOutboxAndPublishesIt()
    {
        Guid accountId = await client.OpenAccountAsync();

        await client.DepositAsync(accountId, 25m);

        OutboxMessage published = await Eventually.GetAsync(
            async () =>
            {
                OutboxMessage? message = await FindOutboxMessageAsync("MoneyDepositedIntegrationEvent", accountId);
                return message?.ProcessedOnUtc is not null ? message : null;
            },
            "The deposit event was not published from the Outbox.");

        Assert.Null(published.Error);
    }

    [Fact]
    public async Task FailedWithdrawal_StoresNoEvent()
    {
        Guid accountId = await client.OpenAccountAsync();

        await client.PostIdempotentAsync($"/accounts/{accountId}/withdrawals", new { amount = 1m });

        // The business rule rejected the operation, so nothing may reach other services.
        Assert.Null(await FindOutboxMessageAsync("MoneyWithdrawnIntegrationEvent", accountId));
    }

    private async Task<OutboxMessage?> FindOutboxMessageAsync(string eventTypeName, Guid accountId)
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventArchDbContext>();

        // Few rows per test run, so filtering the JSON in memory keeps the query simple.
        List<OutboxMessage> candidates = await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message => message.Type.Contains(eventTypeName))
            .ToListAsync();

        return candidates.FirstOrDefault(message => message.Content.Contains(accountId.ToString()));
    }
}
