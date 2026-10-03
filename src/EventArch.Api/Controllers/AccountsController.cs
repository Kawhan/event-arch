using EventArch.Api.Errors;
using EventArch.Api.Idempotency;
using EventArch.Application.Abstractions;
using EventArch.Application.Accounts.CloseAccount;
using EventArch.Application.Accounts.Deposit;
using EventArch.Application.Accounts.FreezeAccount;
using EventArch.Application.Accounts.GetAccount;
using EventArch.Application.Accounts.OpenAccount;
using EventArch.Application.Accounts.UnfreezeAccount;
using EventArch.Application.Accounts.Withdraw;
using EventArch.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace EventArch.Api.Controllers;

/// <summary>
/// Account operations. Each action only translates HTTP into a command or query;
/// all business decisions happen in the handlers and the domain.
/// Handlers are injected per action so each request builds only what it uses.
/// </summary>
[ApiController]
[Route("accounts")]
public sealed class AccountsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<OpenAccountResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Open(
        OpenAccountRequest request,
        [FromServices] ICommandHandler<OpenAccountCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await handler.HandleAsync(new OpenAccountCommand(request.HolderName), cancellationToken);
        if (result.IsFailure)
        {
            return this.ToProblem(result.Error!);
        }

        return CreatedAtAction(nameof(GetById), new { accountId = result.Value }, new OpenAccountResponse(result.Value));
    }

    [HttpGet("{accountId:guid}")]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid accountId,
        [FromServices] IQueryHandler<GetAccountQuery, AccountResponse> handler,
        CancellationToken cancellationToken)
    {
        Result<AccountResponse> result = await handler.HandleAsync(new GetAccountQuery(accountId), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    [HttpPost("{accountId:guid}/deposits")]
    [Idempotent]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Deposit(
        Guid accountId,
        AmountRequest request,
        [FromServices] ICommandHandler<DepositCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(new DepositCommand(accountId, request.Amount), cancellationToken);

        return ToNoContentOrProblem(result);
    }

    [HttpPost("{accountId:guid}/withdrawals")]
    [Idempotent]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Withdraw(
        Guid accountId,
        AmountRequest request,
        [FromServices] ICommandHandler<WithdrawCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(new WithdrawCommand(accountId, request.Amount), cancellationToken);

        return ToNoContentOrProblem(result);
    }

    [HttpPost("{accountId:guid}/freeze")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Freeze(
        Guid accountId,
        [FromServices] ICommandHandler<FreezeAccountCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(new FreezeAccountCommand(accountId), cancellationToken);

        return ToNoContentOrProblem(result);
    }

    [HttpPost("{accountId:guid}/unfreeze")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unfreeze(
        Guid accountId,
        [FromServices] ICommandHandler<UnfreezeAccountCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(new UnfreezeAccountCommand(accountId), cancellationToken);

        return ToNoContentOrProblem(result);
    }

    [HttpPost("{accountId:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Close(
        Guid accountId,
        [FromServices] ICommandHandler<CloseAccountCommand> handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(new CloseAccountCommand(accountId), cancellationToken);

        return ToNoContentOrProblem(result);
    }

    private IActionResult ToNoContentOrProblem(Result result)
    {
        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error!);
    }
}
