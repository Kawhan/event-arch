using EventArch.Contracts;
using EventArch.Contracts.Accounts;
using EventArch.Domain.Accounts.Events;
using EventArch.Domain.Common;

namespace EventArch.Infrastructure.Outbox;

/// <summary>
/// Translates internal domain events into public integration events.
/// Keeping both separate lets the domain evolve without breaking consumers.
/// </summary>
internal static class IntegrationEventMapper
{
    public static IntegrationEvent Map(IDomainEvent domainEvent) => domainEvent switch
    {
        AccountOpened e => new AccountOpenedIntegrationEvent(
            e.EventId, e.OccurredOnUtc, e.AccountId, e.HolderName),

        MoneyDeposited e => new MoneyDepositedIntegrationEvent(
            e.EventId, e.OccurredOnUtc, e.AccountId, e.Amount, e.BalanceAfter, e.TransferId),

        MoneyWithdrawn e => new MoneyWithdrawnIntegrationEvent(
            e.EventId, e.OccurredOnUtc, e.AccountId, e.Amount, e.BalanceAfter, e.TransferId),

        MoneyTransferred e => new MoneyTransferredIntegrationEvent(
            e.EventId, e.OccurredOnUtc, e.TransferId, e.SourceAccountId, e.DestinationAccountId, e.Amount),

        AccountFrozen e => new AccountFrozenIntegrationEvent(e.EventId, e.OccurredOnUtc, e.AccountId),

        AccountUnfrozen e => new AccountUnfrozenIntegrationEvent(e.EventId, e.OccurredOnUtc, e.AccountId),

        AccountClosed e => new AccountClosedIntegrationEvent(e.EventId, e.OccurredOnUtc, e.AccountId),

        // Fails fast when a new domain event is added without a mapping.
        _ => throw new NotSupportedException($"No integration event mapped for '{domainEvent.GetType().Name}'.")
    };
}
