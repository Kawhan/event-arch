namespace EventArch.Application.Abstractions;

/// <summary>
/// Commits every pending change in a single transaction.
/// The implementation also stores the aggregates' domain events in the Outbox,
/// so state and events are saved together or not at all.
/// </summary>
public interface IUnitOfWork
{
    /// <exception cref="ConcurrencyConflictException">Another request saved the same aggregate first.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
