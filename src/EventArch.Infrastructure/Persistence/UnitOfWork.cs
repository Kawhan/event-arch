using System.Diagnostics;
using EventArch.Application.Abstractions;
using EventArch.Domain.Common;
using EventArch.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace EventArch.Infrastructure.Persistence;

/// <summary>
/// Saves aggregates and their events atomically: the domain events of every tracked
/// aggregate become <see cref="OutboxMessage"/> rows in the same database transaction.
/// </summary>
internal sealed class UnitOfWork(EventArchDbContext dbContext) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        AddDomainEventsToOutbox();

        try
        {
            // A single SaveChanges call runs inside one transaction.
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(exception);
        }
    }

    private void AddDomainEventsToOutbox()
    {
        List<AggregateRoot> aggregates = dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        // ASP.NET Core starts an Activity per request; its trace id links logs and messages.
        string? correlationId = Activity.Current?.TraceId.ToString();

        foreach (AggregateRoot aggregate in aggregates)
        {
            foreach (IDomainEvent domainEvent in aggregate.DomainEvents)
            {
                var integrationEvent = IntegrationEventMapper.Map(domainEvent);
                dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent, correlationId));
            }

            aggregate.ClearDomainEvents();
        }
    }
}
