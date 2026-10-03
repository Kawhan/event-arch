namespace EventArch.Domain.Common;

/// <summary>
/// A business fact that already happened inside an aggregate.
/// Names are always in the past tense (e.g. <c>MoneyDeposited</c>).
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }

    DateTime OccurredOnUtc { get; }
}

/// <summary>
/// Convenience base that fills the event metadata automatically.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
