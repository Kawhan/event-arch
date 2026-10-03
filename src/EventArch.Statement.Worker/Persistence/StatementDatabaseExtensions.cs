using Microsoft.EntityFrameworkCore;

namespace EventArch.Statement.Worker.Persistence;

public static class StatementDatabaseExtensions
{
    /// <summary>
    /// Npgsql setup shared by runtime and design time. The migrations history table
    /// also goes to the "statement" schema, so it never collides with the API's.
    /// </summary>
    public static void UseStatementNpgsql(this DbContextOptionsBuilder options, string connectionString)
    {
        options.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", StatementDbContext.Schema));
    }

    /// <summary>
    /// Applies pending migrations. Meant for local development only.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StatementDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
