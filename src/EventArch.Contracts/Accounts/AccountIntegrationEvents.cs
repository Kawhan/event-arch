namespace EventArch.Contracts.Accounts;

public sealed record AccountOpenedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid AccountId,
    string HolderName) : IntegrationEvent(EventId, OccurredOnUtc);

public sealed record MoneyDepositedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid AccountId,
    decimal Amount,
    decimal BalanceAfter,
    Guid? TransferId) : IntegrationEvent(EventId, OccurredOnUtc);

public sealed record MoneyWithdrawnIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid AccountId,
    decimal Amount,
    decimal BalanceAfter,
    Guid? TransferId) : IntegrationEvent(EventId, OccurredOnUtc);

public sealed record MoneyTransferredIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TransferId,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount) : IntegrationEvent(EventId, OccurredOnUtc);

public sealed record AccountFrozenIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid AccountId) : IntegrationEvent(EventId, OccurredOnUtc);

public sealed record AccountUnfrozenIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid AccountId) : IntegrationEvent(EventId, OccurredOnUtc);

public sealed record AccountClosedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid AccountId) : IntegrationEvent(EventId, OccurredOnUtc);
