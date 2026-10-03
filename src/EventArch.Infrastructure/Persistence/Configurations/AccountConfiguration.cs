using EventArch.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventArch.Infrastructure.Persistence.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(account => account.Id);
        builder.Property(account => account.Id).HasColumnName("id");

        builder.Property(account => account.HolderName)
            .HasColumnName("holder_name")
            .HasMaxLength(200)
            .IsRequired();

        // Money is stored as a plain numeric column and rebuilt through its factory.
        builder.Property(account => account.Balance)
            .HasColumnName("balance")
            .HasColumnType("numeric(18,2)")
            .HasConversion(
                money => money.Value,
                value => Money.Create(value).Value);

        // Stored as text so the database stays readable and enum reordering is harmless.
        builder.Property(account => account.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20);

        // Optimistic concurrency using PostgreSQL's system column "xmin":
        // if two requests load the same account, the second save fails instead of
        // silently overwriting the first one (e.g. two withdrawals of the same balance).
        builder.Property<uint>("RowVersion").IsRowVersion();

        // Events are transient; they go to the Outbox, never to the accounts table.
        builder.Ignore(account => account.DomainEvents);
    }
}
