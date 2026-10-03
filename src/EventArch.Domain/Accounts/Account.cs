using EventArch.Domain.Accounts.Events;
using EventArch.Domain.Common;

namespace EventArch.Domain.Accounts;

/// <summary>
/// A bank account in BRL. Aggregate root that guards every balance rule:
/// the balance never goes negative and only active accounts move money.
/// </summary>
public sealed class Account : AggregateRoot
{
    private Account(Guid id, string holderName)
        : base(id)
    {
        HolderName = holderName;
        Balance = Money.Zero;
        Status = AccountStatus.Active;
    }

    // Required by EF Core to rebuild the aggregate from the database.
    private Account()
    {
        HolderName = string.Empty;
        Balance = Money.Zero;
    }

    public string HolderName { get; private set; }

    public Money Balance { get; private set; }

    public AccountStatus Status { get; private set; }

    /// <summary>
    /// Opens a new active account with a zero balance.
    /// </summary>
    public static Result<Account> Open(string holderName)
    {
        if (string.IsNullOrWhiteSpace(holderName))
        {
            return AccountErrors.HolderNameRequired;
        }

        // Version 7 GUIDs are time-ordered, which keeps database indexes compact.
        var account = new Account(Guid.CreateVersion7(), holderName.Trim());
        account.Raise(new AccountOpened(account.Id, account.HolderName));

        return account;
    }

    /// <summary>
    /// Adds money to the balance.
    /// </summary>
    /// <param name="transferId">Set when this deposit is the credit side of a transfer.</param>
    public Result Deposit(Money amount, Guid? transferId = null)
    {
        Result canMove = CanMoveMoney(amount);
        if (canMove.IsFailure)
        {
            return canMove;
        }

        Balance += amount;
        Raise(new MoneyDeposited(Id, amount.Value, Balance.Value, transferId));

        return Result.Success();
    }

    /// <summary>
    /// Withdraws money from the account. The balance can never become negative.
    /// </summary>
    /// <param name="transferId">Set when this withdrawal is the debit side of a transfer.</param>
    public Result Withdraw(Money amount, Guid? transferId = null)
    {
        Result canMove = CanMoveMoney(amount);
        if (canMove.IsFailure)
        {
            return canMove;
        }

        if (amount > Balance)
        {
            return AccountErrors.InsufficientFunds;
        }

        Balance -= amount;
        Raise(new MoneyWithdrawn(Id, amount.Value, Balance.Value, transferId));

        return Result.Success();
    }

    /// <summary>
    /// Blocks every money movement until the account is unfrozen.
    /// </summary>
    public Result Freeze()
    {
        if (Status != AccountStatus.Active)
        {
            return Status == AccountStatus.Closed ? AccountErrors.Closed : AccountErrors.NotActive;
        }

        Status = AccountStatus.Frozen;
        Raise(new AccountFrozen(Id));

        return Result.Success();
    }

    /// <summary>
    /// Allows a frozen account to move money again.
    /// </summary>
    public Result Unfreeze()
    {
        if (Status != AccountStatus.Frozen)
        {
            return Status == AccountStatus.Closed ? AccountErrors.Closed : AccountErrors.NotFrozen;
        }

        Status = AccountStatus.Active;
        Raise(new AccountUnfrozen(Id));

        return Result.Success();
    }

    /// <summary>
    /// Closes the account for good. Requires a zero balance so no money is lost.
    /// </summary>
    public Result Close()
    {
        if (Status == AccountStatus.Closed)
        {
            return AccountErrors.Closed;
        }

        if (!Balance.IsZero)
        {
            return AccountErrors.BalanceNotZero;
        }

        Status = AccountStatus.Closed;
        Raise(new AccountClosed(Id));

        return Result.Success();
    }

    /// <summary>
    /// Checks whether this account may move the given amount right now,
    /// without changing anything. Balance is checked separately by <see cref="Withdraw"/>.
    /// </summary>
    public Result CanMoveMoney(Money amount)
    {
        if (amount.IsZero)
        {
            return AccountErrors.AmountMustBePositive;
        }

        return Status switch
        {
            AccountStatus.Frozen => AccountErrors.Frozen,
            AccountStatus.Closed => AccountErrors.Closed,
            _ => Result.Success()
        };
    }

    /// <summary>
    /// Records the transfer summary on the source account.
    /// Only <see cref="TransferService"/> calls it, after both sides succeeded.
    /// </summary>
    internal void RecordTransferSent(Guid transferId, Guid destinationAccountId, Money amount)
    {
        Raise(new MoneyTransferred(transferId, Id, destinationAccountId, amount.Value));
    }
}
