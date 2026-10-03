using EventArch.Domain.Accounts;
using EventArch.Domain.Accounts.Events;

namespace EventArch.Domain.Tests;

public class AccountTests
{
    // ----- Open -----

    [Fact]
    public void Open_WithHolderName_CreatesActiveAccountWithZeroBalance()
    {
        var result = Account.Open("  Jane Doe  ");

        Account account = result.Value;
        Assert.Equal("Jane Doe", account.HolderName);
        Assert.Equal(Money.Zero, account.Balance);
        Assert.Equal(AccountStatus.Active, account.Status);
    }

    [Fact]
    public void Open_WithHolderName_RaisesAccountOpened()
    {
        Account account = Account.Open("Jane Doe").Value;

        var opened = Assert.IsType<AccountOpened>(Assert.Single(account.DomainEvents));
        Assert.Equal(account.Id, opened.AccountId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Open_WithBlankHolderName_ReturnsHolderNameRequired(string holderName)
    {
        var result = Account.Open(holderName);

        Assert.Equal(AccountErrors.HolderNameRequired, result.Error);
    }

    // ----- Deposit -----

    [Fact]
    public void Deposit_WithPositiveAmount_IncreasesBalanceAndRaisesEvent()
    {
        Account account = TestData.AccountWithBalance(100m);

        var result = account.Deposit(TestData.Money(50m));

        Assert.True(result.IsSuccess);
        Assert.Equal(150m, account.Balance.Value);
        var deposited = Assert.IsType<MoneyDeposited>(Assert.Single(account.DomainEvents));
        Assert.Equal(50m, deposited.Amount);
        Assert.Equal(150m, deposited.BalanceAfter);
        Assert.Null(deposited.TransferId);
    }

    [Fact]
    public void Deposit_WithZeroAmount_ReturnsAmountMustBePositive()
    {
        Account account = TestData.AccountWithBalance();

        var result = account.Deposit(Money.Zero);

        Assert.Equal(AccountErrors.AmountMustBePositive, result.Error);
        Assert.Empty(account.DomainEvents);
    }

    [Fact]
    public void Deposit_WhenFrozen_ReturnsFrozenAndKeepsBalance()
    {
        Account account = TestData.AccountWithBalance(100m);
        account.Freeze();

        var result = account.Deposit(TestData.Money(50m));

        Assert.Equal(AccountErrors.Frozen, result.Error);
        Assert.Equal(100m, account.Balance.Value);
    }

    // ----- Withdraw -----

    [Fact]
    public void Withdraw_WithEnoughBalance_DecreasesBalanceAndRaisesEvent()
    {
        Account account = TestData.AccountWithBalance(100m);

        var result = account.Withdraw(TestData.Money(30m));

        Assert.True(result.IsSuccess);
        Assert.Equal(70m, account.Balance.Value);
        var withdrawn = Assert.IsType<MoneyWithdrawn>(Assert.Single(account.DomainEvents));
        Assert.Equal(30m, withdrawn.Amount);
        Assert.Equal(70m, withdrawn.BalanceAfter);
    }

    [Fact]
    public void Withdraw_WholeBalance_LeavesZero()
    {
        Account account = TestData.AccountWithBalance(100m);

        account.Withdraw(TestData.Money(100m));

        Assert.True(account.Balance.IsZero);
    }

    [Fact]
    public void Withdraw_WhenAmountExceedsBalance_ReturnsInsufficientFunds()
    {
        Account account = TestData.AccountWithBalance(100m);

        var result = account.Withdraw(TestData.Money(100.01m));

        Assert.Equal(AccountErrors.InsufficientFunds, result.Error);
        Assert.Equal(100m, account.Balance.Value);
        Assert.Empty(account.DomainEvents);
    }

    [Fact]
    public void Withdraw_WhenClosed_ReturnsClosed()
    {
        Account account = TestData.AccountWithBalance();
        account.Close();

        var result = account.Withdraw(TestData.Money(1m));

        Assert.Equal(AccountErrors.Closed, result.Error);
    }

    // ----- Freeze / Unfreeze -----

    [Fact]
    public void Freeze_WhenActive_FreezesAndRaisesEvent()
    {
        Account account = TestData.AccountWithBalance();

        var result = account.Freeze();

        Assert.True(result.IsSuccess);
        Assert.Equal(AccountStatus.Frozen, account.Status);
        Assert.IsType<AccountFrozen>(Assert.Single(account.DomainEvents));
    }

    [Fact]
    public void Freeze_WhenAlreadyFrozen_ReturnsNotActive()
    {
        Account account = TestData.AccountWithBalance();
        account.Freeze();

        var result = account.Freeze();

        Assert.Equal(AccountErrors.NotActive, result.Error);
    }

    [Fact]
    public void Unfreeze_WhenFrozen_ReactivatesAndRaisesEvent()
    {
        Account account = TestData.AccountWithBalance();
        account.Freeze();
        account.ClearDomainEvents();

        var result = account.Unfreeze();

        Assert.True(result.IsSuccess);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.IsType<AccountUnfrozen>(Assert.Single(account.DomainEvents));
    }

    [Fact]
    public void Unfreeze_WhenActive_ReturnsNotFrozen()
    {
        Account account = TestData.AccountWithBalance();

        var result = account.Unfreeze();

        Assert.Equal(AccountErrors.NotFrozen, result.Error);
    }

    // ----- Close -----

    [Fact]
    public void Close_WithZeroBalance_ClosesAndRaisesEvent()
    {
        Account account = TestData.AccountWithBalance();

        var result = account.Close();

        Assert.True(result.IsSuccess);
        Assert.Equal(AccountStatus.Closed, account.Status);
        Assert.IsType<AccountClosed>(Assert.Single(account.DomainEvents));
    }

    [Fact]
    public void Close_WhenFrozenWithZeroBalance_Closes()
    {
        Account account = TestData.AccountWithBalance();
        account.Freeze();

        var result = account.Close();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Close_WithRemainingBalance_ReturnsBalanceNotZero()
    {
        Account account = TestData.AccountWithBalance(0.01m);

        var result = account.Close();

        Assert.Equal(AccountErrors.BalanceNotZero, result.Error);
        Assert.Equal(AccountStatus.Active, account.Status);
    }

    [Fact]
    public void Close_WhenAlreadyClosed_ReturnsClosed()
    {
        Account account = TestData.AccountWithBalance();
        account.Close();

        var result = account.Close();

        Assert.Equal(AccountErrors.Closed, result.Error);
    }
}
