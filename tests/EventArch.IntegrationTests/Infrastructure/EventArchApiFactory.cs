using EventArch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace EventArch.IntegrationTests.Infrastructure;

/// <summary>
/// Runs the real API in memory against disposable PostgreSQL and RabbitMQ containers.
/// One instance is shared by every test in the "Api" collection, so containers start once.
/// </summary>
public sealed class EventArchApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Same images as docker-compose.yml, so tests run against the versions used locally.
    private readonly PostgreSqlContainer postgresContainer = new PostgreSqlBuilder("postgres:17").Build();

    private readonly RabbitMqContainer rabbitMqContainer = new RabbitMqBuilder("rabbitmq:4-management").Build();

    public string RabbitMqConnectionString => rabbitMqContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(postgresContainer.StartAsync(), rabbitMqContainer.StartAsync());

        // Program only migrates in Development; tests run as "Testing", so migrate here.
        await Services.ApplyMigrationsAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:Database", postgresContainer.GetConnectionString());
        builder.UseSetting("ConnectionStrings:RabbitMq", rabbitMqContainer.GetConnectionString());

        // Publish quickly so tests that wait for messages stay fast.
        builder.UseSetting("Outbox:PollingInterval", "00:00:00.200");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
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
