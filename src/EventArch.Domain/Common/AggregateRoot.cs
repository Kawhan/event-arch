namespace EventArch.Domain.Common;

/// <summary>
/// Consistency boundary of the domain. Collects the domain events raised by its
/// operations so the infrastructure can persist them (Outbox) in the same transaction.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> domainEvents = [];

    protected AggregateRoot(Guid id)
        : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => domainEvents.AsReadOnly();

    /// <summary>
    /// Removes the collected events. Called after they were safely stored.
    /// </summary>
    public void ClearDomainEvents() => domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => domainEvents.Add(domainEvent);
}
