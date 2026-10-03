namespace EventArch.Contracts;

/// <summary>
/// Base of every message published to other services.
/// Integration events are a public contract: change them only in backward-compatible ways.
/// </summary>
/// <param name="EventId">Unique id; consumers use it to ignore duplicates.</param>
/// <param name="OccurredOnUtc">When the business fact happened.</param>
public abstract record IntegrationEvent(Guid EventId, DateTime OccurredOnUtc);
