using EventArch.Domain.Common;

namespace EventArch.Application.Abstractions;

/// <summary>
/// Handles a command that changes state and returns no value.
/// </summary>
public interface ICommandHandler<in TCommand>
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Handles a command that changes state and returns a value (e.g. the new id).
/// </summary>
public interface ICommandHandler<in TCommand, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
