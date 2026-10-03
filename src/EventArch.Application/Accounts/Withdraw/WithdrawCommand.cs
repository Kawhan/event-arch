namespace EventArch.Application.Accounts.Withdraw;

/// <summary>
/// Takes money out of an account. Fails when the balance is not enough.
/// </summary>
public sealed record WithdrawCommand(Guid AccountId, decimal Amount);
