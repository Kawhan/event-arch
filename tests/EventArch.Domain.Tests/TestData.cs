using EventArch.Domain.Accounts;

namespace EventArch.Domain.Tests;

/// <summary>
/// Shortcuts that keep the tests focused on behavior instead of setup.
/// </summary>
internal static class TestData
{
    public static Money Money(decimal value) => Accounts.Money.Create(value).Value;

    /// <summary>
    /// An active account with the given balance and no pending events,
    /// so each test only sees the events raised by the action under test.
    /// </summary>
    public static Account AccountWithBalance(decimal balance = 0m)
    {
        Account account = Account.Open("Jane Doe").Value;

        if (balance > 0m)
        {
            account.Deposit(Money(balance));
        }

        account.ClearDomainEvents();
        return account;
    }
}
