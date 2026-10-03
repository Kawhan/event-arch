using EventArch.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventArch.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).HasColumnName("id");
        builder.Property(message => message.Type).HasColumnName("type").HasMaxLength(500);
        builder.Property(message => message.Content).HasColumnName("content").HasColumnType("jsonb");
        builder.Property(message => message.OccurredOnUtc).HasColumnName("occurred_on_utc");
        builder.Property(message => message.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        builder.Property(message => message.ProcessedOnUtc).HasColumnName("processed_on_utc");
        builder.Property(message => message.Error).HasColumnName("error");
        builder.Property(message => message.Attempts).HasColumnName("attempts");
        builder.Property(message => message.DeadLetteredOnUtc).HasColumnName("dead_lettered_on_utc");

        // Partial index: the publisher only ever looks for pending messages that are still alive.
        builder.HasIndex(message => message.OccurredOnUtc)
            .HasDatabaseName("ix_outbox_messages_pending")
            .HasFilter("processed_on_utc IS NULL AND dead_lettered_on_utc IS NULL");
    }
}
