namespace EventArch.Infrastructure.Outbox;

/// <summary>
/// Settings of the Outbox publisher, bound from the "Outbox" configuration section.
/// </summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>How long to wait between polls when there is nothing to publish.</summary>
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>Maximum number of messages published per transaction.</summary>
    public int BatchSize { get; init; } = 50;

    /// <summary>Failed attempts after which a message is dead-lettered and no longer retried.</summary>
    public int MaxAttempts { get; init; } = 10;
}
