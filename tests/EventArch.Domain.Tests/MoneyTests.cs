using EventArch.Domain.Accounts;

namespace EventArch.Domain.Tests;

public class MoneyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(10.5)]
    [InlineData(10.55)]
    public void Create_WithValidValue_Succeeds(decimal value)
    {
        var result = Money.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value.Value);
    }

    [Fact]
    public void Create_WithNegativeValue_ReturnsNegativeError()
    {
        var result = Money.Create(-0.01m);

        Assert.Equal(MoneyErrors.Negative, result.Error);
    }

    [Fact]
    public void Create_WithFractionOfCent_ReturnsTooManyDecimalPlacesError()
    {
        var result = Money.Create(10.001m);

        Assert.Equal(MoneyErrors.TooManyDecimalPlaces, result.Error);
    }

    [Fact]
    public void Add_ReturnsSumOfBothAmounts()
    {
        Money sum = TestData.Money(10.25m) + TestData.Money(4.75m);

        Assert.Equal(15m, sum.Value);
    }

    [Fact]
    public void Subtract_WhenResultWouldBeNegative_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => TestData.Money(1m) - TestData.Money(2m));
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        Assert.Equal(TestData.Money(7m), TestData.Money(7m));
    }
}
