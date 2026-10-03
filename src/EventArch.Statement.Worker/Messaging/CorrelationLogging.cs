using Rebus.Messages;
using Rebus.Pipeline;
using Serilog.Context;

namespace EventArch.Statement.Worker.Messaging;

internal static class CorrelationLogging
{
    /// <summary>
    /// Adds the incoming message's correlation id to every log written inside the scope.
    /// It is the trace id of the original API request, so both sides can be found together in Seq.
    /// </summary>
    public static IDisposable BeginCorrelationScope(this IMessageContext messageContext)
    {
        string correlationId = messageContext.Headers.GetValueOrDefault(Headers.CorrelationId) ?? "unknown";
        return LogContext.PushProperty("CorrelationId", correlationId);
    }
}
