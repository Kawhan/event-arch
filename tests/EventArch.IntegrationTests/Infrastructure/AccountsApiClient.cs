using System.Net;
using System.Net.Http.Json;

namespace EventArch.IntegrationTests.Infrastructure;

/// <summary>
/// Shortcuts for the HTTP calls most tests need as setup.
/// Each helper asserts its own success, so a broken setup fails loudly and early.
/// </summary>
internal static class AccountsApiClient
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    private sealed record OpenAccountResponse(Guid AccountId);

    private sealed record AccountBody(Guid Id, decimal Balance, string Status);

    public static async Task<Guid> OpenAccountAsync(this HttpClient client, string holderName = "Jane Doe")
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/accounts", new { holderName });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<OpenAccountResponse>();
        return body!.AccountId;
    }

    public static async Task DepositAsync(this HttpClient client, Guid accountId, decimal amount)
    {
        HttpResponseMessage response = await client.PostIdempotentAsync($"/accounts/{accountId}/deposits", new { amount });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public static async Task<decimal> GetBalanceAsync(this HttpClient client, Guid accountId)
    {
        var account = await client.GetFromJsonAsync<AccountBody>($"/accounts/{accountId}");
        return account!.Balance;
    }

    /// <summary>
    /// POSTs JSON with an Idempotency-Key header. A new key is generated unless one is given,
    /// which is what a well-behaved client does for every new operation.
    /// </summary>
    public static Task<HttpResponseMessage> PostIdempotentAsync(
        this HttpClient client, string url, object body, string? idempotencyKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Add(IdempotencyKeyHeader, idempotencyKey ?? Guid.NewGuid().ToString());

        return client.SendAsync(request);
    }
}
