using EventArch.Domain.Accounts;
using EventArch.Infrastructure.Idempotency;
using EventArch.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace EventArch.Infrastructure.Persistence;

/// <summary>
/// EF Core session over the write database. Mappings live in <c>Configurations/</c>.
/// </summary>
public sealed class EventArchDbContext(DbContextOptions<EventArchDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<IdempotencyKeyEntry> IdempotencyKeys => Set<IdempotencyKeyEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventArchDbContext).Assembly);
    }
}
