using EventArch.Contracts.Accounts;
using EventArch.Statement.Worker.Messaging;
using EventArch.Statement.Worker.Persistence;
using EventArch.Statement.Worker.Statements;
using Rebus.Bus;
using Rebus.Config;

namespace EventArch.Statement.Worker;

/// <summary>
/// Registers everything the statement worker needs: its database, the recorder,
/// the message handlers and the Rebus consumer. Shared by Program and the tests.
/// </summary>
public static class DependencyInjection
{
    public const string DefaultQueueName = "eventarch.statement";

    public static IServiceCollection AddStatementWorker(this IServiceCollection services, IConfiguration configuration)
    {
        string databaseConnection = GetRequiredConnectionString(configuration, "Database");
        string rabbitMqConnection = GetRequiredConnectionString(configuration, "RabbitMq");
        string queueName = configuration["Rebus:QueueName"] ?? DefaultQueueName;

        services.AddDbContext<StatementDbContext>(options => options.UseStatementNpgsql(databaseConnection));
        services.AddScoped<StatementRecorder>();

        // Handlers are listed explicitly so it is obvious which events this service reacts to.
        services.AddRebusHandler<MoneyDepositedHandler>();
        services.AddRebusHandler<MoneyWithdrawnHandler>();

        // Rebus retries a failing message 5 times, then moves it to the "error" queue.
        services.AddRebus(
            configure => configure.Transport(transport => transport.UseRabbitMq(rabbitMqConnection, queueName)),
            onCreated: SubscribeToStatementEventsAsync);

        return services;
    }

    /// <summary>
    /// Binds the worker's queue to every event it handles. Safe to call more than once.
    /// </summary>
    public static async Task SubscribeToStatementEventsAsync(IBus bus)
    {
        await bus.Subscribe<MoneyDepositedIntegrationEvent>();
        await bus.Subscribe<MoneyWithdrawnIntegrationEvent>();
    }

    private static string GetRequiredConnectionString(IConfiguration configuration, string name)
    {
        return configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Connection string '{name}' is not configured.");
    }
}
