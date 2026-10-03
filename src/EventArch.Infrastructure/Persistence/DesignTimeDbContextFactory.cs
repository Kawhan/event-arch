using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EventArch.Infrastructure.Persistence;

/// <summary>
/// Used only by the "dotnet ef" tool to create migrations, so this project
/// does not need the API to build a DbContext. Never used at runtime.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EventArchDbContext>
{
    // Matches docker-compose. Override with EVENTARCH_DATABASE to target another database.
    private const string LocalConnectionString =
        "Host=localhost;Port=5432;Database=eventarch;Username=eventarch;Password=eventarch";

    public EventArchDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("EVENTARCH_DATABASE") ?? LocalConnectionString;

        var options = new DbContextOptionsBuilder<EventArchDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new EventArchDbContext(options);
    }
}
