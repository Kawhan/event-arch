namespace EventArch.Application.Abstractions;

/// <summary>
/// Remembers the response of each request sent with an idempotency key, so a retried
/// request (e.g. after a timeout) returns the original response instead of running again.
/// </summary>
public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> FindAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Starts a transaction that wraps the operation itself. Everything the operation saves
    /// and the idempotency record are committed together, or not at all.
    /// </summary>
    Task<IIdempotencyTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}

/// <summary>
/// A transaction opened by <see cref="IIdempotencyStore.BeginTransactionAsync"/>.
/// Disposing it without <see cref="CommitAsync"/> rolls everything back.
/// </summary>
public interface IIdempotencyTransaction : IAsyncDisposable
{
    /// <summary>
    /// Stores the record and commits the operation in the same transaction.
    /// </summary>
    /// <exception cref="IdempotencyKeyConflictException">Another request stored the same key first.</exception>
    Task CommitAsync(IdempotencyRecord record, CancellationToken cancellationToken);
}

/// <summary>
/// The stored outcome of a request.
/// </summary>
/// <param name="Key">The client's Idempotency-Key header.</param>
/// <param name="RequestHash">Fingerprint of the request, to detect a key reused for a different request.</param>
/// <param name="StatusCode">HTTP status code of the original response.</param>
/// <param name="ResponseBody">JSON body of the original response; null when it had none.</param>
public sealed record IdempotencyRecord(string Key, string RequestHash, int StatusCode, string? ResponseBody);

/// <summary>
/// Two requests with the same idempotency key ran at the same time; the other one won.
/// </summary>
public sealed class IdempotencyKeyConflictException(Exception innerException)
    : Exception("A request with the same Idempotency-Key is being processed. Retry later.", innerException);
