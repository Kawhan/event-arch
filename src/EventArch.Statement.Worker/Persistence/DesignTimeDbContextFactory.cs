using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EventArch.Statement.Worker.Persistence;

/// <summary>
/// Used only by the "dotnet ef" tool to create migrations. Never used at runtime.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<StatementDbContext>
{
    // Matches docker-compose. Override with EVENTARCH_DATABASE to target another database.
    private const string LocalConnectionString =
        "Host=localhost;Port=5432;Database=eventarch;Username=eventarch;Password=eventarch";

    public StatementDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("EVENTARCH_DATABASE") ?? LocalConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<StatementDbContext>();
        optionsBuilder.UseStatementNpgsql(connectionString);

        return new StatementDbContext(optionsBuilder.Options);
    }
}
