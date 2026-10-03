using EventArch.Statement.Worker.Statements;
using Microsoft.EntityFrameworkCore;

namespace EventArch.Statement.Worker.Persistence;

/// <summary>
/// The worker's own storage. It lives in the "statement" schema and is never
/// shared with the API: services only talk to each other through events.
/// </summary>
public sealed class StatementDbContext(DbContextOptions<StatementDbContext> options) : DbContext(options)
{
    public const string Schema = "statement";

    public DbSet<StatementEntry> StatementEntries => Set<StatementEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<StatementEntry>(builder =>
        {
            builder.ToTable("statement_entries");

            builder.HasKey(entry => entry.EventId);
            builder.Property(entry => entry.EventId).HasColumnName("event_id");
            builder.Property(entry => entry.AccountId).HasColumnName("account_id");
            builder.Property(entry => entry.OccurredOnUtc).HasColumnName("occurred_on_utc");
            builder.Property(entry => entry.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(10);
            builder.Property(entry => entry.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
            builder.Property(entry => entry.BalanceAfter).HasColumnName("balance_after").HasColumnType("numeric(18,2)");
            builder.Property(entry => entry.TransferId).HasColumnName("transfer_id");

            // Statements are always read per account, newest first.
            builder.HasIndex(entry => new { entry.AccountId, entry.OccurredOnUtc })
                .HasDatabaseName("ix_statement_entries_account_occurred");
        });
    }
}
