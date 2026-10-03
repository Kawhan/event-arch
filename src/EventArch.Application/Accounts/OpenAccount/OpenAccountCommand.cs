namespace EventArch.Application.Accounts.OpenAccount;

/// <summary>
/// Opens a new account for the given holder. Returns the new account id.
/// </summary>
public sealed record OpenAccountCommand(string HolderName);
