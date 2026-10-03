namespace EventArch.Application.Accounts.CloseAccount;

/// <summary>
/// Closes the account for good. Requires a zero balance.
/// </summary>
public sealed record CloseAccountCommand(Guid AccountId);
