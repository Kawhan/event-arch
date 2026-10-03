using EventArch.Contracts;
using EventArch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rebus.Bus;
using Rebus.Messages;

namespace EventArch.Infrastructure.Outbox;

/// <summary>
/// Background loop that publishes pending Outbox messages to RabbitMQ through Rebus.
/// Delivery is at-least-once: if marking a message fails after publishing it,
/// it is published again, so consumers must be idempotent.
/// </summary>
internal sealed class OutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxPublisherWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox publisher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            int publishedCount = 0;

            try
            {
                publishedCount = await PublishPendingBatchAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Never let the loop die: a database or broker outage must only delay publishing.
                logger.LogError(exception, "Outbox publishing cycle failed");
            }

            // A full batch means there may be more waiting, so poll again right away.
            if (publishedCount < options.Value.BatchSize)
            {
                await Task.Delay(options.Value.PollingInterval, timeProvider, stoppingToken);
            }
        }
    }

    /// <returns>How many messages were published.</returns>
    private async Task<int> PublishPendingBatchAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventArchDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // SKIP LOCKED lets several API instances share the work without publishing
        // the same message twice: rows locked by one instance are invisible to the others.
        List<OutboxMessage> pendingMessages = await dbContext.OutboxMessages
            .FromSql($"""
                SELECT * FROM outbox_messages
                WHERE processed_on_utc IS NULL
                ORDER BY occurred_on_utc
                LIMIT {options.Value.BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        int publishedCount = 0;

        foreach (OutboxMessage message in pendingMessages)
        {
            try
            {
                IntegrationEvent integrationEvent = message.Deserialize();
                await bus.Publish(integrationEvent, BuildHeaders(message));

                message.MarkAsProcessed(timeProvider.GetUtcNow().UtcDateTime);
                publishedCount++;
            }
            catch (Exception exception)
            {
                // Keep the message pending; it is retried on the next cycle.
                message.MarkAsFailed(exception.Message);
                logger.LogWarning(exception, "Failed to publish outbox message {OutboxMessageId}", message.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (publishedCount > 0)
        {
            logger.LogInformation("Published {PublishedCount} outbox messages", publishedCount);
        }

        return publishedCount;
    }

    private static Dictionary<string, string> BuildHeaders(OutboxMessage message)
    {
        var headers = new Dictionary<string, string>
        {
            // Rebus uses the message id to identify each delivery.
            [Headers.MessageId] = message.Id.ToString()
        };

        if (message.CorrelationId is not null)
        {
            headers[Headers.CorrelationId] = message.CorrelationId;
        }

        return headers;
    }
}
