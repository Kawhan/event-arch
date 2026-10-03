using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventArch.Infrastructure.Persistence;

public static class MigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations. Meant for local development only;
    /// production databases should be migrated by the deployment pipeline.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventArchDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
