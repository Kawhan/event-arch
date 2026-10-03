using System.Text;
using System.Text.Json;
using EventArch.Contracts.Accounts;
using EventArch.IntegrationTests.Infrastructure;
using RabbitMQ.Client;

namespace EventArch.IntegrationTests;

/// <summary>
/// Proves that events really reach RabbitMQ, the way any consumer would receive them.
/// </summary>
[Collection(ApiCollection.Name)]
public class MessagingTests(EventArchApiFactory factory)
{
    // Rebus publishes every event to this topic exchange, routed by the event type name.
    private const string RebusTopicsExchange = "RebusTopics";

    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Deposit_PublishesMoneyDepositedToRabbitMq()
    {
        var connectionFactory = new ConnectionFactory { Uri = new Uri(factory.RabbitMqConnectionString) };
        await using IConnection connection = await connectionFactory.CreateConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();

        // A temporary queue subscribed to deposits, declared before the deposit happens.
        await channel.ExchangeDeclareAsync(RebusTopicsExchange, ExchangeType.Topic, durable: true);
        QueueDeclareOk queue = await channel.QueueDeclareAsync();
        string topic = $"{typeof(MoneyDepositedIntegrationEvent).FullName}, {typeof(MoneyDepositedIntegrationEvent).Assembly.GetName().Name}";
        await channel.QueueBindAsync(queue.QueueName, RebusTopicsExchange, topic);

        Guid accountId = await client.OpenAccountAsync();
        await client.DepositAsync(accountId, 42m);

        MoneyDepositedIntegrationEvent received = await Eventually.GetAsync(
            async () => await ReceiveDepositForAccountAsync(channel, queue.QueueName, accountId),
            "No MoneyDeposited message arrived on RabbitMQ.");

        Assert.Equal(42m, received.Amount);
        Assert.Equal(42m, received.BalanceAfter);
    }

    /// <summary>
    /// Reads pending messages and returns the deposit for the given account, if it arrived.
    /// Messages from other tests may share the exchange, so they are skipped.
    /// </summary>
    private static async Task<MoneyDepositedIntegrationEvent?> ReceiveDepositForAccountAsync(
        IChannel channel, string queueName, Guid accountId)
    {
        while (await channel.BasicGetAsync(queueName, autoAck: true) is { } delivery)
        {
            string json = Encoding.UTF8.GetString(delivery.Body.Span);

            // Web defaults are case-insensitive, so the test does not depend on Rebus' naming policy.
            var deposit = JsonSerializer.Deserialize<MoneyDepositedIntegrationEvent>(
                json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (deposit?.AccountId == accountId)
            {
                return deposit;
            }
        }

        return null;
    }
}
