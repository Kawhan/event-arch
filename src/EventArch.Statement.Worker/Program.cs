using EventArch.Contracts.Accounts;
using EventArch.Statement.Worker.Messaging;
using EventArch.Statement.Worker.Persistence;
using EventArch.Statement.Worker.Statements;
using Rebus.Config;
using Serilog;

// Bootstrap logger: captures errors that happen before the configuration is loaded.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    string databaseConnection = GetRequiredConnectionString(builder.Configuration, "Database");
    string rabbitMqConnection = GetRequiredConnectionString(builder.Configuration, "RabbitMq");
    string queueName = builder.Configuration["Rebus:QueueName"] ?? "eventarch.statement";

    builder.Services.AddDbContext<StatementDbContext>(options => options.UseStatementNpgsql(databaseConnection));
    builder.Services.AddScoped<StatementRecorder>();

    // Handlers are listed explicitly so it is obvious which events this service reacts to.
    builder.Services.AddRebusHandler<MoneyDepositedHandler>();
    builder.Services.AddRebusHandler<MoneyWithdrawnHandler>();

    // Rebus retries a failing message 5 times, then moves it to the "error" queue.
    builder.Services.AddRebus(
        configure => configure.Transport(transport => transport.UseRabbitMq(rabbitMqConnection, queueName)),
        onCreated: async bus =>
        {
            await bus.Subscribe<MoneyDepositedIntegrationEvent>();
            await bus.Subscribe<MoneyWithdrawnIntegrationEvent>();
        });

    var host = builder.Build();

    if (builder.Environment.IsDevelopment())
    {
        await host.Services.ApplyMigrationsAsync();
    }

    await host.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Statement worker terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

static string GetRequiredConnectionString(IConfiguration configuration, string name)
{
    return configuration.GetConnectionString(name)
        ?? throw new InvalidOperationException($"Connection string '{name}' is not configured.");
}
