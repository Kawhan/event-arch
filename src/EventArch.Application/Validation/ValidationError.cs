using EventArch.Domain.Common;

namespace EventArch.Application.Validation;

/// <summary>
/// Input validation failure listing the messages for each invalid field.
/// </summary>
/// <param name="Errors">Field name → messages for that field.</param>
public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Errors)
    : Error("Validation.Failed", "One or more validation errors occurred.", ErrorType.Validation);
