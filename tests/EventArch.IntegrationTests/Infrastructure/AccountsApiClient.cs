using System.Net;
using System.Net.Http.Json;

namespace EventArch.IntegrationTests.Infrastructure;

/// <summary>
/// Shortcuts for the HTTP calls most tests need as setup.
/// Each helper asserts its own success, so a broken setup fails loudly and early.
/// </summary>
internal static class AccountsApiClient
{
    private sealed record OpenAccountResponse(Guid AccountId);

    public static async Task<Guid> OpenAccountAsync(this HttpClient client, string holderName = "Jane Doe")
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/accounts", new { holderName });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<OpenAccountResponse>();
        return body!.AccountId;
    }

    public static async Task DepositAsync(this HttpClient client, Guid accountId, decimal amount)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync($"/accounts/{accountId}/deposits", new { amount });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public static async Task<decimal> GetBalanceAsync(this HttpClient client, Guid accountId)
    {
        var account = await client.GetFromJsonAsync<AccountBody>($"/accounts/{accountId}");
        return account!.Balance;
    }

    private sealed record AccountBody(Guid Id, decimal Balance, string Status);
}
