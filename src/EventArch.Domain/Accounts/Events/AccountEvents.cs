using EventArch.Domain.Common;

namespace EventArch.Domain.Accounts.Events;

// Amounts are carried as plain decimals so events stay easy to serialize into the Outbox.

public sealed record AccountOpened(Guid AccountId, string HolderName) : DomainEvent;

/// <param name="TransferId">Set when the deposit is the credit side of a transfer.</param>
public sealed record MoneyDeposited(Guid AccountId, decimal Amount, decimal BalanceAfter, Guid? TransferId) : DomainEvent;

/// <param name="TransferId">Set when the withdrawal is the debit side of a transfer.</param>
public sealed record MoneyWithdrawn(Guid AccountId, decimal Amount, decimal BalanceAfter, Guid? TransferId) : DomainEvent;

/// <summary>
/// Summary of a completed transfer, raised by the source account.
/// </summary>
public sealed record MoneyTransferred(Guid TransferId, Guid SourceAccountId, Guid DestinationAccountId, decimal Amount) : DomainEvent;

public sealed record AccountFrozen(Guid AccountId) : DomainEvent;

public sealed record AccountUnfrozen(Guid AccountId) : DomainEvent;

public sealed record AccountClosed(Guid AccountId) : DomainEvent;
