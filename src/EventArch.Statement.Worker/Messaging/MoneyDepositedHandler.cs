using EventArch.Contracts.Accounts;
using EventArch.Statement.Worker.Statements;
using Rebus.Handlers;
using Rebus.Pipeline;

namespace EventArch.Statement.Worker.Messaging;

/// <summary>
/// Turns every deposit (including the credit side of a transfer) into a credit line.
/// </summary>
internal sealed class MoneyDepositedHandler(StatementRecorder recorder, IMessageContext messageContext)
    : IHandleMessages<MoneyDepositedIntegrationEvent>
{
    public async Task Handle(MoneyDepositedIntegrationEvent message)
    {
        using IDisposable correlationScope = messageContext.BeginCorrelationScope();

        var entry = StatementEntry.Create(
            message.EventId,
            message.AccountId,
            message.OccurredOnUtc,
            EntryKind.Credit,
            message.Amount,
            message.BalanceAfter,
            message.TransferId);

        await recorder.RecordAsync(entry, messageContext.IncomingStepContext.Load<CancellationToken>());
    }
}
