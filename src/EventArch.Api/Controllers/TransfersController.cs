using EventArch.Api.Errors;
using EventArch.Api.Idempotency;
using EventArch.Application.Abstractions;
using EventArch.Application.Accounts.Transfer;
using EventArch.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace EventArch.Api.Controllers;

/// <summary>
/// Money transfers between accounts. Kept in its own resource because a
/// transfer involves two accounts and does not belong to either one.
/// </summary>
[ApiController]
[Route("transfers")]
public sealed class TransfersController : ControllerBase
{
    [HttpPost]
    [Idempotent]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Transfer(
        TransferRequest request,
        [FromServices] ICommandHandler<TransferCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        var command = new TransferCommand(request.SourceAccountId, request.DestinationAccountId, request.Amount);
        Result<Guid> result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess ? Ok(new TransferResponse(result.Value)) : this.ToProblem(result.Error!);
    }
}
