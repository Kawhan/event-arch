namespace EventArch.Application.Accounts.GetAccount;

/// <summary>
/// Reads the current state of an account.
/// </summary>
public sealed record GetAccountQuery(Guid AccountId);

/// <summary>
/// Account data exposed to clients. Decoupled from the aggregate on purpose,
/// so the domain can change without breaking the API contract.
/// </summary>
public sealed record AccountResponse(Guid Id, string HolderName, decimal Balance, string Currency, string Status);
