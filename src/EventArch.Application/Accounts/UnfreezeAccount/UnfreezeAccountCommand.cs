namespace EventArch.Application.Accounts.UnfreezeAccount;

/// <summary>
/// Allows a frozen account to move money again.
/// </summary>
public sealed record UnfreezeAccountCommand(Guid AccountId);
