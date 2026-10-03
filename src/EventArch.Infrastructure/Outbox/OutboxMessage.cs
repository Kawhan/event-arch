using System.Text.Json;
using EventArch.Contracts;

namespace EventArch.Infrastructure.Outbox;

/// <summary>
/// An integration event waiting to be published. Saved in the same transaction
/// as the aggregate change, so an event is never lost and never published for
/// a change that was rolled back.
/// </summary>
public sealed class OutboxMessage
{
    // Required by EF Core.
    private OutboxMessage()
    {
        Type = string.Empty;
        Content = string.Empty;
    }

    /// <summary>Same value as the integration event's EventId.</summary>
    public Guid Id { get; private set; }

    /// <summary>Type name used to deserialize <see cref="Content"/> ("Namespace.Type, Assembly").</summary>
    public string Type { get; private set; }

    /// <summary>The integration event serialized as JSON.</summary>
    public string Content { get; private set; }

    public DateTime OccurredOnUtc { get; private set; }

    /// <summary>Trace id of the request that produced the event, propagated to consumers.</summary>
    public string? CorrelationId { get; private set; }

    /// <summary>Null while the message is still pending.</summary>
    public DateTime? ProcessedOnUtc { get; private set; }

    /// <summary>Last publishing error, kept for troubleshooting.</summary>
    public string? Error { get; private set; }

    public static OutboxMessage Create(IntegrationEvent integrationEvent, string? correlationId)
    {
        Type eventType = integrationEvent.GetType();

        return new OutboxMessage
        {
            Id = integrationEvent.EventId,
            Type = $"{eventType.FullName}, {eventType.Assembly.GetName().Name}",
            Content = JsonSerializer.Serialize(integrationEvent, eventType),
            OccurredOnUtc = integrationEvent.OccurredOnUtc,
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Rebuilds the integration event stored in this message.
    /// </summary>
    public IntegrationEvent Deserialize()
    {
        Type eventType = System.Type.GetType(Type, throwOnError: true)!;
        return (IntegrationEvent)JsonSerializer.Deserialize(Content, eventType)!;
    }

    public void MarkAsProcessed(DateTime processedOnUtc)
    {
        ProcessedOnUtc = processedOnUtc;
        Error = null;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
    }
}
