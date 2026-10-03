namespace EventArch.Domain.Common;

/// <summary>
/// Category of an error. The API layer uses it to choose the HTTP status code.
/// </summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict
}

/// <summary>
/// Describes why an operation failed, without throwing an exception.
/// </summary>
/// <param name="Code">Stable, machine-readable identifier (e.g. <c>Account.InsufficientFunds</c>).</param>
/// <param name="Message">Human-readable explanation.</param>
/// <param name="Type">Category used to map the error to a response.</param>
/// <remarks>Not sealed so specialized errors can carry extra details (see input validation).</remarks>
public record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
}
