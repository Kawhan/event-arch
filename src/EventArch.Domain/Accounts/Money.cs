using EventArch.Domain.Common;

namespace EventArch.Domain.Accounts;

/// <summary>
/// An amount of money in BRL. Never negative and never more precise than cents.
/// Immutable: every operation returns a new instance.
/// </summary>
public sealed record Money : IComparable<Money>
{
    public const string Currency = "BRL";

    private Money(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; }

    public static Money Zero { get; } = new(0m);

    public bool IsZero => Value == 0m;

    /// <summary>
    /// Creates a money amount, rejecting negative values and fractions of a cent.
    /// </summary>
    public static Result<Money> Create(decimal value)
    {
        if (value < 0m)
        {
            return MoneyErrors.Negative;
        }

        if (decimal.Round(value, 2) != value)
        {
            return MoneyErrors.TooManyDecimalPlaces;
        }

        return new Money(value);
    }

    public static Money operator +(Money left, Money right) => new(left.Value + right.Value);

    /// <summary>
    /// Subtracts two amounts. Callers must check the balance first, so a negative
    /// result means a bug, not a business failure.
    /// </summary>
    public static Money operator -(Money left, Money right)
    {
        decimal difference = left.Value - right.Value;
        if (difference < 0m)
        {
            throw new InvalidOperationException("Money cannot become negative.");
        }

        return new Money(difference);
    }

    public static bool operator >(Money left, Money right) => left.Value > right.Value;

    public static bool operator <(Money left, Money right) => left.Value < right.Value;

    public int CompareTo(Money? other) => other is null ? 1 : Value.CompareTo(other.Value);

    public override string ToString() => $"{Currency} {Value:0.00}";
}

/// <summary>
/// Errors produced while creating <see cref="Money"/>.
/// </summary>
public static class MoneyErrors
{
    public static readonly Error Negative =
        Error.Validation("Money.Negative", "Money amount cannot be negative.");

    public static readonly Error TooManyDecimalPlaces =
        Error.Validation("Money.TooManyDecimalPlaces", "Money amount cannot have more than two decimal places.");
}
