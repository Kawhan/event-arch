using EventArch.Application.Abstractions;
using EventArch.Infrastructure.Idempotency;
using EventArch.Infrastructure.Outbox;
using EventArch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Config;

namespace EventArch.Infrastructure;

/// <summary>
/// Registers database access, the Outbox publisher and the message bus.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string databaseConnection = GetRequiredConnectionString(configuration, "Database");
        string rabbitMqConnection = GetRequiredConnectionString(configuration, "RabbitMq");

        services.AddDbContext<EventArchDbContext>(options => options.UseNpgsql(databaseConnection));
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();

        // The API only publishes, so Rebus runs as a one-way client (no input queue).
        services.AddRebus(configure => configure
            .Transport(transport => transport.UseRabbitMqAsOneWayClient(rabbitMqConnection)));

        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<OutboxPublisherWorker>();

        return services;
    }

    private static string GetRequiredConnectionString(IConfiguration configuration, string name)
    {
        return configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Connection string '{name}' is not configured.");
    }
}
