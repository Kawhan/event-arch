namespace EventArch.Statement.Worker.Statements;

/// <summary>
/// Direction of a statement line from the account's point of view.
/// </summary>
public enum EntryKind
{
    Credit,
    Debit
}

/// <summary>
/// One line of an account statement, built from a money movement event.
/// This is a read model: it only records what the events say, it never decides anything.
/// </summary>
public sealed class StatementEntry
{
    // Required by EF Core.
    private StatementEntry()
    {
    }

    /// <summary>
    /// The id of the event that produced this line. Using it as the primary key makes
    /// recording idempotent: a redelivered event can never create a second line.
    /// </summary>
    public Guid EventId { get; private set; }

    public Guid AccountId { get; private set; }

    public DateTime OccurredOnUtc { get; private set; }

    public EntryKind Kind { get; private set; }

    public decimal Amount { get; private set; }

    public decimal BalanceAfter { get; private set; }

    /// <summary>Set when the line is one side of a transfer.</summary>
    public Guid? TransferId { get; private set; }

    public static StatementEntry Create(
        Guid eventId,
        Guid accountId,
        DateTime occurredOnUtc,
        EntryKind kind,
        decimal amount,
        decimal balanceAfter,
        Guid? transferId)
    {
        return new StatementEntry
        {
            EventId = eventId,
            AccountId = accountId,
            OccurredOnUtc = occurredOnUtc,
            Kind = kind,
            Amount = amount,
            BalanceAfter = balanceAfter,
            TransferId = transferId
        };
    }
}
