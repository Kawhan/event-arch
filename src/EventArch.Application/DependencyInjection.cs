using EventArch.Application.Abstractions;
using EventArch.Application.Accounts.CloseAccount;
using EventArch.Application.Accounts.Deposit;
using EventArch.Application.Accounts.FreezeAccount;
using EventArch.Application.Accounts.GetAccount;
using EventArch.Application.Accounts.OpenAccount;
using EventArch.Application.Accounts.Transfer;
using EventArch.Application.Accounts.UnfreezeAccount;
using EventArch.Application.Accounts.Withdraw;
using EventArch.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EventArch.Application;

/// <summary>
/// Registers every use case of the Application layer.
/// Handlers are listed explicitly so it is obvious which use cases exist.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddCommandHandler<OpenAccountCommand, Guid, OpenAccountHandler>();
        services.AddCommandHandler<DepositCommand, DepositHandler>();
        services.AddCommandHandler<WithdrawCommand, WithdrawHandler>();
        services.AddCommandHandler<TransferCommand, Guid, TransferHandler>();
        services.AddCommandHandler<FreezeAccountCommand, FreezeAccountHandler>();
        services.AddCommandHandler<UnfreezeAccountCommand, UnfreezeAccountHandler>();
        services.AddCommandHandler<CloseAccountCommand, CloseAccountHandler>();

        services.AddScoped<IQueryHandler<GetAccountQuery, AccountResponse>, GetAccountHandler>();

        return services;
    }

    /// <summary>
    /// Registers a command handler wrapped by <see cref="ValidationDecorator{TCommand}"/>,
    /// so callers resolving the interface always get validation first.
    /// </summary>
    private static void AddCommandHandler<TCommand, THandler>(this IServiceCollection services)
        where THandler : class, ICommandHandler<TCommand>
    {
        services.AddScoped<THandler>();
        services.AddScoped<ICommandHandler<TCommand>>(provider => new ValidationDecorator<TCommand>(
            provider.GetRequiredService<THandler>(),
            provider.GetServices<IValidator<TCommand>>()));
    }

    /// <inheritdoc cref="AddCommandHandler{TCommand, THandler}"/>
    private static void AddCommandHandler<TCommand, TResponse, THandler>(this IServiceCollection services)
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        services.AddScoped<THandler>();
        services.AddScoped<ICommandHandler<TCommand, TResponse>>(provider => new ValidationDecorator<TCommand, TResponse>(
            provider.GetRequiredService<THandler>(),
            provider.GetServices<IValidator<TCommand>>()));
    }
}
