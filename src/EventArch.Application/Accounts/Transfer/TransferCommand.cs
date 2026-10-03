namespace EventArch.Application.Accounts.Transfer;

/// <summary>
/// Moves money between two accounts. Returns the transfer id.
/// </summary>
public sealed record TransferCommand(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount);
