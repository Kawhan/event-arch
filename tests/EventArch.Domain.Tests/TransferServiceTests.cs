using EventArch.Domain.Accounts;
using EventArch.Domain.Accounts.Events;

namespace EventArch.Domain.Tests;

public class TransferServiceTests
{
    [Fact]
    public void Transfer_WithEnoughBalance_MovesMoneyBetweenAccounts()
    {
        Account source = TestData.AccountWithBalance(100m);
        Account destination = TestData.AccountWithBalance(10m);

        var result = TransferService.Transfer(source, destination, TestData.Money(40m));

        Assert.True(result.IsSuccess);
        Assert.Equal(60m, source.Balance.Value);
        Assert.Equal(50m, destination.Balance.Value);
    }

    [Fact]
    public void Transfer_WithEnoughBalance_RaisesEventsSharingTheTransferId()
    {
        Account source = TestData.AccountWithBalance(100m);
        Account destination = TestData.AccountWithBalance();

        Guid transferId = TransferService.Transfer(source, destination, TestData.Money(40m)).Value;

        var withdrawn = Assert.Single(source.DomainEvents.OfType<MoneyWithdrawn>());
        var transferred = Assert.Single(source.DomainEvents.OfType<MoneyTransferred>());
        var deposited = Assert.IsType<MoneyDeposited>(Assert.Single(destination.DomainEvents));

        Assert.Equal(transferId, withdrawn.TransferId);
        Assert.Equal(transferId, deposited.TransferId);
        Assert.Equal(transferId, transferred.TransferId);
        Assert.Equal(destination.Id, transferred.DestinationAccountId);
    }

    [Fact]
    public void Transfer_ToSameAccount_ReturnsSameAccountTransfer()
    {
        Account account = TestData.AccountWithBalance(100m);

        var result = TransferService.Transfer(account, account, TestData.Money(10m));

        Assert.Equal(AccountErrors.SameAccountTransfer, result.Error);
        Assert.Equal(100m, account.Balance.Value);
    }

    [Fact]
    public void Transfer_WhenDestinationIsFrozen_FailsWithoutDebitingSource()
    {
        Account source = TestData.AccountWithBalance(100m);
        Account destination = TestData.AccountWithBalance();
        destination.Freeze();

        var result = TransferService.Transfer(source, destination, TestData.Money(10m));

        Assert.Equal(AccountErrors.Frozen, result.Error);
        Assert.Equal(100m, source.Balance.Value);
        Assert.Empty(source.DomainEvents);
    }

    [Fact]
    public void Transfer_WithInsufficientFunds_FailsWithoutCreditingDestination()
    {
        Account source = TestData.AccountWithBalance(5m);
        Account destination = TestData.AccountWithBalance();

        var result = TransferService.Transfer(source, destination, TestData.Money(10m));

        Assert.Equal(AccountErrors.InsufficientFunds, result.Error);
        Assert.True(destination.Balance.IsZero);
        Assert.Empty(destination.DomainEvents);
    }
}
