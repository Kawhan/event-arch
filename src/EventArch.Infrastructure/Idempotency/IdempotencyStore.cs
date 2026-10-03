using EventArch.Application.Abstractions;
using EventArch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace EventArch.Infrastructure.Idempotency;

internal sealed class IdempotencyStore(EventArchDbContext dbContext, TimeProvider timeProvider) : IIdempotencyStore
{
    public async Task<IdempotencyRecord?> FindAsync(string key, CancellationToken cancellationToken)
    {
        IdempotencyKeyEntry? entry = await dbContext.IdempotencyKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.Key == key, cancellationToken);

        return entry is null
            ? null
            : new IdempotencyRecord(entry.Key, entry.RequestHash, entry.StatusCode, entry.ResponseBody);
    }

    public async Task<IIdempotencyTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        // The same scoped DbContext is used by the handlers, so their SaveChanges
        // calls join this transaction instead of committing on their own.
        IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new IdempotencyTransaction(dbContext, transaction, timeProvider);
    }

    private sealed class IdempotencyTransaction(
        EventArchDbContext dbContext,
        IDbContextTransaction transaction,
        TimeProvider timeProvider) : IIdempotencyTransaction
    {
        public async Task CommitAsync(IdempotencyRecord record, CancellationToken cancellationToken)
        {
            dbContext.IdempotencyKeys.Add(IdempotencyKeyEntry.Create(
                record.Key,
                record.RequestHash,
                record.StatusCode,
                record.ResponseBody,
                timeProvider.GetUtcNow().UtcDateTime));

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsDuplicateKey(exception))
            {
                // Disposing the transaction rolls back the operation as well.
                throw new IdempotencyKeyConflictException(exception);
            }
        }

        public ValueTask DisposeAsync() => transaction.DisposeAsync();

        private static bool IsDuplicateKey(DbUpdateException exception)
        {
            return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
        }
    }
}
