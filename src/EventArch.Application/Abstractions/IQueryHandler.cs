using EventArch.Domain.Common;

namespace EventArch.Application.Abstractions;

/// <summary>
/// Handles a read-only request. Must never change state.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
