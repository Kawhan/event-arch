using EventArch.Api.Controllers;
using EventArch.Infrastructure.Persistence;
using EventArch.Statement.Worker;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rebus.Bus;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using WorkerMigrations = EventArch.Statement.Worker.Persistence.StatementDatabaseExtensions;

namespace EventArch.IntegrationTests.Infrastructure;

/// <summary>
/// Runs the whole system for the tests: the real API in memory and the statement worker
/// as a hosted service, both against disposable PostgreSQL and RabbitMQ containers.
/// One instance is shared by every test in the "Api" collection, so containers start once.
/// </summary>
// Any API type works as the entry point marker. Program is not used because the API and
// the worker both generate a global "Program" class, which would be ambiguous here.
public sealed class EventArchApiFactory : WebApplicationFactory<AccountsController>, IAsyncLifetime
{
    // Same images as docker-compose.yml, so tests run against the versions used locally.
    private readonly PostgreSqlContainer postgresContainer = new PostgreSqlBuilder("postgres:17").Build();

    private readonly RabbitMqContainer rabbitMqContainer = new RabbitMqBuilder("rabbitmq:4-management").Build();

    private IHost? workerHost;

    public string RabbitMqConnectionString => rabbitMqContainer.GetConnectionString();

    /// <summary>
    /// Services of the running statement worker (its DbContext, recorder and bus).
    /// </summary>
    public IServiceProvider WorkerServices =>
        workerHost?.Services ?? throw new InvalidOperationException("The worker has not started.");

    public async Task InitializeAsync()
    {
        await Task.WhenAll(postgresContainer.StartAsync(), rabbitMqContainer.StartAsync());

        // Program only migrates in Development; tests run as "Testing", so migrate here.
        await Services.ApplyMigrationsAsync();

        workerHost = await StartWorkerAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:Database", postgresContainer.GetConnectionString());
        builder.UseSetting("ConnectionStrings:RabbitMq", rabbitMqContainer.GetConnectionString());

        // Publish quickly so tests that wait for messages stay fast.
        builder.UseSetting("Outbox:PollingInterval", "00:00:00.200");
    }

    private async Task<IHost> StartWorkerAsync()
    {
        // Empty builder: no appsettings files, only the settings below.
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(
            new HostApplicationBuilderSettings { EnvironmentName = "Testing" });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = postgresContainer.GetConnectionString(),
            ["ConnectionStrings:RabbitMq"] = rabbitMqContainer.GetConnectionString()
        });

        builder.Services.AddStatementWorker(builder.Configuration);

        IHost host = builder.Build();
        await WorkerMigrations.ApplyMigrationsAsync(host.Services);
        await host.StartAsync();

        // Rebus subscribes in the background while starting. Subscribing again and awaiting it
        // guarantees the queue is bound before any test publishes; otherwise RabbitMQ would
        // silently drop the first events.
        await DependencyInjection.SubscribeToStatementEventsAsync(host.Services.GetRequiredService<IBus>());

        return host;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        // Stop consumers first, then the API, and only then remove the infrastructure they use.
        if (workerHost is not null)
        {
            await workerHost.StopAsync();
            workerHost.Dispose();
        }

        await DisposeAsync();
        await Task.WhenAll(postgresContainer.DisposeAsync().AsTask(), rabbitMqContainer.DisposeAsync().AsTask());
    }
}

/// <summary>
/// Groups the tests that share <see cref="EventArchApiFactory"/>.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<EventArchApiFactory>
{
    public const string Name = "Api";
}
