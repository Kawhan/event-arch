using EventArch.Contracts.Accounts;
using EventArch.Statement.Worker.Statements;
using Rebus.Handlers;
using Rebus.Pipeline;

namespace EventArch.Statement.Worker.Messaging;

/// <summary>
/// Turns every withdrawal (including the debit side of a transfer) into a debit line.
/// </summary>
internal sealed class MoneyWithdrawnHandler(StatementRecorder recorder, IMessageContext messageContext)
    : IHandleMessages<MoneyWithdrawnIntegrationEvent>
{
    public async Task Handle(MoneyWithdrawnIntegrationEvent message)
    {
        using IDisposable correlationScope = messageContext.BeginCorrelationScope();

        var entry = StatementEntry.Create(
            message.EventId,
            message.AccountId,
            message.OccurredOnUtc,
            EntryKind.Debit,
            message.Amount,
            message.BalanceAfter,
            message.TransferId);

        await recorder.RecordAsync(entry, messageContext.IncomingStepContext.Load<CancellationToken>());
    }
}
