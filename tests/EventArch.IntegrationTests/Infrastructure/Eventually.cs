namespace EventArch.IntegrationTests.Infrastructure;

/// <summary>
/// Waits for asynchronous work (Outbox publishing, message delivery) to happen.
/// </summary>
internal static class Eventually
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Polls <paramref name="probe"/> until it returns a value, or fails the test on timeout.
    /// </summary>
    public static async Task<T> GetAsync<T>(Func<Task<T?>> probe, string failureMessage, TimeSpan? timeout = null)
        where T : class
    {
        DateTime deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);

        while (DateTime.UtcNow < deadline)
        {
            T? value = await probe();
            if (value is not null)
            {
                return value;
            }

            await Task.Delay(PollingInterval);
        }

        Assert.Fail(failureMessage);
        return null!;
    }
}
