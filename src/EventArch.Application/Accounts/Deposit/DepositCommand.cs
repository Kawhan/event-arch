namespace EventArch.Application.Accounts.Deposit;

/// <summary>
/// Adds money to an account.
/// </summary>
public sealed record DepositCommand(Guid AccountId, decimal Amount);
