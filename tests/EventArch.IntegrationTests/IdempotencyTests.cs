using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventArch.IntegrationTests.Infrastructure;

namespace EventArch.IntegrationTests;

/// <summary>
/// Retrying a money operation with the same Idempotency-Key must never move money twice.
/// </summary>
[Collection(ApiCollection.Name)]
public class IdempotencyTests(EventArchApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Deposit_WithoutIdempotencyKey_Returns400()
    {
        Guid accountId = await client.OpenAccountAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync($"/accounts/{accountId}/deposits", new { amount = 10m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Idempotency.KeyRequired", await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Deposit_RetriedWithSameKey_CreditsOnlyOnceAndReplaysResponse()
    {
        Guid accountId = await client.OpenAccountAsync();
        string key = Guid.NewGuid().ToString();
        string url = $"/accounts/{accountId}/deposits";

        HttpResponseMessage first = await client.PostIdempotentAsync(url, new { amount = 50m }, key);
        HttpResponseMessage retry = await client.PostIdempotentAsync(url, new { amount = 50m }, key);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, retry.StatusCode);
        Assert.True(retry.Headers.Contains("Idempotency-Replayed"));
        Assert.Equal(50m, await client.GetBalanceAsync(accountId));
    }

    [Fact]
    public async Task Transfer_RetriedWithSameKey_ReturnsTheOriginalTransferId()
    {
        Guid sourceId = await client.OpenAccountAsync("Source");
        Guid destinationId = await client.OpenAccountAsync("Destination");
        await client.DepositAsync(sourceId, 100m);
        string key = Guid.NewGuid().ToString();
        var body = new { sourceAccountId = sourceId, destinationAccountId = destinationId, amount = 30m };

        HttpResponseMessage first = await client.PostIdempotentAsync("/transfers", body, key);
        HttpResponseMessage retry = await client.PostIdempotentAsync("/transfers", body, key);

        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(await ReadTransferIdAsync(first), await ReadTransferIdAsync(retry));
        Assert.Equal(70m, await client.GetBalanceAsync(sourceId));
        Assert.Equal(30m, await client.GetBalanceAsync(destinationId));
    }

    [Fact]
    public async Task SameKey_WithDifferentBody_Returns422AndDoesNotRun()
    {
        Guid accountId = await client.OpenAccountAsync();
        string key = Guid.NewGuid().ToString();
        string url = $"/accounts/{accountId}/deposits";

        await client.PostIdempotentAsync(url, new { amount = 50m }, key);
        HttpResponseMessage reused = await client.PostIdempotentAsync(url, new { amount = 999m }, key);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, reused.StatusCode);
        Assert.Equal("Idempotency.KeyReused", await ReadErrorCodeAsync(reused));
        Assert.Equal(50m, await client.GetBalanceAsync(accountId));
    }

    [Fact]
    public async Task FailedOperation_IsReplayedWithTheSameError()
    {
        Guid accountId = await client.OpenAccountAsync();
        string key = Guid.NewGuid().ToString();
        string url = $"/accounts/{accountId}/withdrawals";

        HttpResponseMessage first = await client.PostIdempotentAsync(url, new { amount = 10m }, key);

        // Money arrives in between: the retry must still answer as the original request did.
        await client.DepositAsync(accountId, 100m);
        HttpResponseMessage retry = await client.PostIdempotentAsync(url, new { amount = 10m }, key);

        Assert.Equal(HttpStatusCode.Conflict, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode);
        Assert.Equal("Account.InsufficientFunds", await ReadErrorCodeAsync(retry));
        Assert.Equal(100m, await client.GetBalanceAsync(accountId));
    }

    [Fact]
    public async Task ConcurrentRequestsWithSameKey_CreditOnlyOnce()
    {
        Guid accountId = await client.OpenAccountAsync();
        string key = Guid.NewGuid().ToString();
        string url = $"/accounts/{accountId}/deposits";

        // Simulates a client firing the same request several times at once (double click, aggressive retry).
        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => client.PostIdempotentAsync(url, new { amount = 20m }, key)));

        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.NoContent);
        Assert.All(responses, response => Assert.True(
            response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.Conflict,
            $"Unexpected status {(int)response.StatusCode}"));
        Assert.Equal(20m, await client.GetBalanceAsync(accountId));
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return problem.RootElement.GetProperty("code").GetString();
    }

    private static async Task<Guid> ReadTransferIdAsync(HttpResponseMessage response)
    {
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("transferId").GetGuid();
    }
}
