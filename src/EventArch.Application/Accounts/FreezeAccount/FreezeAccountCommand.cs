namespace EventArch.Application.Accounts.FreezeAccount;

/// <summary>
/// Blocks every money movement on the account.
/// </summary>
public sealed record FreezeAccountCommand(Guid AccountId);
