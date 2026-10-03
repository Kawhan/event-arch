namespace EventArch.Domain.Common;

/// <summary>
/// Outcome of an operation that can fail for expected business reasons.
/// </summary>
public class Result
{
    protected Result(Error? error)
    {
        Error = error;
    }

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public bool IsFailure => !IsSuccess;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);

    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);

    // Lets a method simply "return SomeErrors.X;" instead of "return Result.Failure(...)".
    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>
/// Outcome of an operation that produces a value when it succeeds.
/// </summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? value;

    private Result(TValue? value, Error? error)
        : base(error)
    {
        this.value = value;
    }

    /// <summary>
    /// The produced value. Reading it from a failed result is a programming error.
    /// </summary>
    public TValue Value => IsSuccess
        ? value!
        : throw new InvalidOperationException("Cannot read the value of a failed result.");

    public static Result<TValue> Success(TValue value) => new(value, null);

    public static new Result<TValue> Failure(Error error) => new(default, error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure(error);
}
