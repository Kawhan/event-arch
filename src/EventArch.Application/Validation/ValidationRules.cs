using FluentValidation;

namespace EventArch.Application.Validation;

/// <summary>
/// Reusable input rules, so every command validates the same concept the same way.
/// </summary>
internal static class ValidationRules
{
    public const int HolderNameMaxLength = 200;

    /// <summary>
    /// A money amount sent by a client: positive and with at most two decimal places.
    /// </summary>
    public static IRuleBuilderOptions<T, decimal> MustBeValidAmount<T>(this IRuleBuilder<T, decimal> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThan(0m)
            .WithMessage("Amount must be greater than zero.")
            .Must(amount => decimal.Round(amount, 2) == amount)
            .WithMessage("Amount cannot have more than two decimal places.");
    }
}
