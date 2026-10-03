using EventArch.Infrastructure.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventArch.Infrastructure.Persistence.Configurations;

internal sealed class IdempotencyKeyEntryConfiguration : IEntityTypeConfiguration<IdempotencyKeyEntry>
{
    public const int KeyMaxLength = 100;

    public void Configure(EntityTypeBuilder<IdempotencyKeyEntry> builder)
    {
        builder.ToTable("idempotency_keys");

        builder.HasKey(entry => entry.Key);
        builder.Property(entry => entry.Key).HasColumnName("key").HasMaxLength(KeyMaxLength);
        builder.Property(entry => entry.RequestHash).HasColumnName("request_hash").HasMaxLength(64);
        builder.Property(entry => entry.StatusCode).HasColumnName("status_code");
        builder.Property(entry => entry.ResponseBody).HasColumnName("response_body");
        builder.Property(entry => entry.CreatedOnUtc).HasColumnName("created_on_utc");
    }
}
