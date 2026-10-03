using EventArch.Domain.Common;

namespace EventArch.Domain.Accounts;

/// <summary>
/// Catalog of business errors related to <see cref="Account"/>.
/// </summary>
public static class AccountErrors
{
    public static readonly Error HolderNameRequired =
        Error.Validation("Account.HolderNameRequired", "The account holder name is required.");

    public static readonly Error AmountMustBePositive =
        Error.Validation("Account.AmountMustBePositive", "The amount must be greater than zero.");

    public static readonly Error InsufficientFunds =
        Error.Conflict("Account.InsufficientFunds", "The account balance is not enough for this operation.");

    public static readonly Error Frozen =
        Error.Conflict("Account.Frozen", "The account is frozen and cannot move money.");

    public static readonly Error Closed =
        Error.Conflict("Account.Closed", "The account is closed.");

    public static readonly Error NotActive =
        Error.Conflict("Account.NotActive", "Only active accounts can be frozen.");

    public static readonly Error NotFrozen =
        Error.Conflict("Account.NotFrozen", "Only frozen accounts can be unfrozen.");

    public static readonly Error BalanceNotZero =
        Error.Conflict("Account.BalanceNotZero", "An account can only be closed with a zero balance.");

    public static readonly Error SameAccountTransfer =
        Error.Validation("Account.SameAccountTransfer", "Source and destination accounts must be different.");

    public static Error NotFound(Guid accountId) =>
        Error.NotFound("Account.NotFound", $"Account '{accountId}' was not found.");
}
