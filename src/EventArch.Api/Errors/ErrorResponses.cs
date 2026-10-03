using EventArch.Application.Validation;
using EventArch.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace EventArch.Api.Errors;

/// <summary>
/// Translates domain and application errors into RFC 7807 ProblemDetails responses.
/// This is the only place that decides which HTTP status each error type gets.
/// </summary>
public static class ErrorResponses
{
    public static IActionResult ToProblem(this ControllerBase controller, Error error)
    {
        if (error is ValidationError validationError)
        {
            var modelState = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
            foreach ((string field, string[] messages) in validationError.Errors)
            {
                foreach (string message in messages)
                {
                    modelState.AddModelError(field, message);
                }
            }

            return controller.ValidationProblem(modelState);
        }

        int statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        ObjectResult problem = controller.Problem(detail: error.Message, statusCode: statusCode);

        // The stable code lets clients react to a specific error without parsing the message.
        if (problem.Value is ProblemDetails problemDetails)
        {
            problemDetails.Extensions["code"] = error.Code;
        }

        return problem;
    }
}
