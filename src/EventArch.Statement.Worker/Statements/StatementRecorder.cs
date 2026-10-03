using EventArch.Statement.Worker.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventArch.Statement.Worker.Statements;

/// <summary>
/// Stores statement lines exactly once, even though messages are delivered at least once.
/// </summary>
public sealed class StatementRecorder(StatementDbContext dbContext, ILogger<StatementRecorder> logger)
{
    public async Task RecordAsync(StatementEntry entry, CancellationToken cancellationToken)
    {
        bool alreadyRecorded = await dbContext.StatementEntries
            .AnyAsync(existing => existing.EventId == entry.EventId, cancellationToken);

        if (alreadyRecorded)
        {
            LogDuplicate(entry);
            return;
        }

        dbContext.StatementEntries.Add(entry);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            // Another delivery of the same event won the race between the check and the insert.
            LogDuplicate(entry);
            return;
        }

        logger.LogInformation(
            "Recorded {EntryKind} of {Amount} for account {AccountId}",
            entry.Kind, entry.Amount, entry.AccountId);
    }

    private void LogDuplicate(StatementEntry entry)
    {
        logger.LogInformation("Event {EventId} was already recorded; ignoring duplicate", entry.EventId);
    }

    private static bool IsDuplicateKey(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
