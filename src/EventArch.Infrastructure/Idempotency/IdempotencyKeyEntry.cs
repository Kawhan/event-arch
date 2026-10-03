namespace EventArch.Infrastructure.Idempotency;

/// <summary>
/// Database row of an idempotency key. The key is the primary key, which is what
/// guarantees that two concurrent requests cannot both commit the same key.
/// </summary>
public sealed class IdempotencyKeyEntry
{
    // Required by EF Core.
    private IdempotencyKeyEntry()
    {
        Key = string.Empty;
        RequestHash = string.Empty;
    }

    public string Key { get; private set; }

    public string RequestHash { get; private set; }

    public int StatusCode { get; private set; }

    public string? ResponseBody { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public static IdempotencyKeyEntry Create(
        string key, string requestHash, int statusCode, string? responseBody, DateTime createdOnUtc)
    {
        return new IdempotencyKeyEntry
        {
            Key = key,
            RequestHash = requestHash,
            StatusCode = statusCode,
            ResponseBody = responseBody,
            CreatedOnUtc = createdOnUtc
        };
    }
}
