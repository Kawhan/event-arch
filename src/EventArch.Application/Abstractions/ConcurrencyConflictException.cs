namespace EventArch.Application.Abstractions;

/// <summary>
/// Thrown by <see cref="IUnitOfWork"/> when another request changed the same
/// aggregate first. The client should reload and try again (HTTP 409).
/// </summary>
public sealed class ConcurrencyConflictException(Exception innerException)
    : Exception("The data was changed by another request. Reload and try again.", innerException);
