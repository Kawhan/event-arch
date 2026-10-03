using EventArch.Infrastructure.Outbox;
using EventArch.Infrastructure.Persistence;
using EventArch.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventArch.IntegrationTests;

/// <summary>
/// Messages that can never be published must not block the Outbox forever.
/// </summary>
[Collection(SmallOutboxCollection.Name)]
public class OutboxDeadLetterTests(SmallOutboxFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task FullBatchOfBrokenMessages_IsDeadLetteredAndValidMessagesStillPublish()
    {
        // A whole batch of messages whose type no longer exists, older than anything else,
        // so every cycle picks them first. Before dead-lettering, this blocked the Outbox for good.
        List<Guid> brokenIds = await InsertBrokenMessagesAsync(SmallOutboxFactory.BatchSize);

        Guid accountId = await client.OpenAccountAsync();
        await client.DepositAsync(accountId, 10m);

        await Eventually.GetAsync(
            async () =>
            {
                OutboxMessage? deposit = await FindDepositMessageAsync(accountId);
                return deposit?.ProcessedOnUtc is not null ? deposit : null;
            },
            "The valid deposit was never published: broken messages are still blocking the Outbox.");

        List<OutboxMessage> brokenMessages = await GetMessagesAsync(brokenIds);
        Assert.All(brokenMessages, message =>
        {
            Assert.NotNull(message.DeadLetteredOnUtc);
            Assert.Null(message.ProcessedOnUtc);
            Assert.Equal(SmallOutboxFactory.MaxAttempts, message.Attempts);
            Assert.NotNull(message.Error);
        });
    }

    private async Task<List<Guid>> InsertBrokenMessagesAsync(int count)
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventArchDbContext>();

        var ids = new List<Guid>();
        DateTime oneHourAgo = DateTime.UtcNow.AddHours(-1);
        const string removedEventType = "EventArch.Contracts.RemovedEvent, EventArch.Contracts";
        const string emptyJson = "{}";

        for (int index = 0; index < count; index++)
        {
            var id = Guid.NewGuid();
            ids.Add(id);

            // Raw SQL on purpose: the application has no way to create a broken message.
            await dbContext.Database.ExecuteSqlAsync($"""
                INSERT INTO outbox_messages (id, type, content, occurred_on_utc, attempts)
                VALUES ({id}, {removedEventType}, {emptyJson}::jsonb, {oneHourAgo}, 0)
                """);
        }

        return ids;
    }

    private async Task<OutboxMessage?> FindDepositMessageAsync(Guid accountId)
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventArchDbContext>();

        List<OutboxMessage> deposits = await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message => message.Type.Contains("MoneyDepositedIntegrationEvent"))
            .ToListAsync();

        return deposits.FirstOrDefault(message => message.Content.Contains(accountId.ToString()));
    }

    private async Task<List<OutboxMessage>> GetMessagesAsync(List<Guid> ids)
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventArchDbContext>();

        return await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message => ids.Contains(message.Id))
            .ToListAsync();
    }
}
