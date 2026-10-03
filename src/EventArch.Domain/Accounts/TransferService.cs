using EventArch.Domain.Common;

namespace EventArch.Domain.Accounts;

/// <summary>
/// Domain service that moves money between two accounts.
/// A transfer touches two aggregates, so the rule does not belong to either one alone.
/// </summary>
public static class TransferService
{
    /// <summary>
    /// Debits <paramref name="source"/> and credits <paramref name="destination"/>.
    /// Either both sides change or neither does.
    /// </summary>
    /// <returns>The id shared by every event of this transfer.</returns>
    public static Result<Guid> Transfer(Account source, Account destination, Money amount)
    {
        if (source.Id == destination.Id)
        {
            return AccountErrors.SameAccountTransfer;
        }

        // Validate the destination before touching the source, so a failure
        // never leaves the source debited in memory.
        Result destinationCheck = destination.CanMoveMoney(amount);
        if (destinationCheck.IsFailure)
        {
            return destinationCheck.Error!;
        }

        var transferId = Guid.CreateVersion7();

        Result withdrawal = source.Withdraw(amount, transferId);
        if (withdrawal.IsFailure)
        {
            return withdrawal.Error!;
        }

        Result deposit = destination.Deposit(amount, transferId);
        if (deposit.IsFailure)
        {
            // Unreachable while CanMoveMoney covers every Deposit rule; guards future changes.
            throw new InvalidOperationException($"Deposit failed after validation: {deposit.Error!.Code}.");
        }

        source.RecordTransferSent(transferId, destination.Id, amount);

        return transferId;
    }
}
