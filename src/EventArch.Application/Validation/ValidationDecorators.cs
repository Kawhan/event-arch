using EventArch.Application.Abstractions;
using EventArch.Domain.Common;
using FluentValidation;

namespace EventArch.Application.Validation;

/// <summary>
/// Runs every validator of the command before the real handler.
/// Invalid input never reaches the handler; it becomes a <see cref="ValidationError"/>.
/// </summary>
internal sealed class ValidationDecorator<TCommand>(
    ICommandHandler<TCommand> innerHandler,
    IEnumerable<IValidator<TCommand>> validators)
    : ICommandHandler<TCommand>
{
    public async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        ValidationError? validationError = await CommandValidator.ValidateAsync(command, validators, cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        return await innerHandler.HandleAsync(command, cancellationToken);
    }
}

/// <inheritdoc cref="ValidationDecorator{TCommand}"/>
internal sealed class ValidationDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> innerHandler,
    IEnumerable<IValidator<TCommand>> validators)
    : ICommandHandler<TCommand, TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        ValidationError? validationError = await CommandValidator.ValidateAsync(command, validators, cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        return await innerHandler.HandleAsync(command, cancellationToken);
    }
}

/// <summary>
/// Validation logic shared by both decorators.
/// </summary>
internal static class CommandValidator
{
    /// <returns>The validation error, or <c>null</c> when the command is valid.</returns>
    public static async Task<ValidationError?> ValidateAsync<TCommand>(
        TCommand command,
        IEnumerable<IValidator<TCommand>> validators,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TCommand>(command);
        var failures = new List<FluentValidation.Results.ValidationFailure>();

        foreach (IValidator<TCommand> validator in validators)
        {
            var validationResult = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(validationResult.Errors);
        }

        if (failures.Count == 0)
        {
            return null;
        }

        Dictionary<string, string[]> errorsByField = failures
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());

        return new ValidationError(errorsByField);
    }
}
