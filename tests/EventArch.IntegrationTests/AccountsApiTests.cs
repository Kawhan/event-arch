using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventArch.IntegrationTests.Infrastructure;

namespace EventArch.IntegrationTests;

/// <summary>
/// End-to-end HTTP behavior: routing, validation, business errors and persistence.
/// </summary>
[Collection(ApiCollection.Name)]
public class AccountsApiTests(EventArchApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task OpenAccount_ReturnsCreatedWithLocationOfTheNewAccount()
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/accounts", new { holderName = "Jane Doe" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        HttpResponseMessage getResponse = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task DepositAndWithdraw_UpdateTheStoredBalance()
    {
        Guid accountId = await client.OpenAccountAsync();

        await client.DepositAsync(accountId, 100m);
        HttpResponseMessage withdrawal = await client.PostIdempotentAsync(
            $"/accounts/{accountId}/withdrawals", new { amount = 30.50m });

        Assert.Equal(HttpStatusCode.NoContent, withdrawal.StatusCode);
        Assert.Equal(69.50m, await client.GetBalanceAsync(accountId));
    }

    [Fact]
    public async Task Withdraw_AboveBalance_Returns409WithErrorCode()
    {
        Guid accountId = await client.OpenAccountAsync();
        await client.DepositAsync(accountId, 10m);

        HttpResponseMessage response = await client.PostIdempotentAsync(
            $"/accounts/{accountId}/withdrawals", new { amount = 10.01m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Account.InsufficientFunds", await ReadErrorCodeAsync(response));
        Assert.Equal(10m, await client.GetBalanceAsync(accountId));
    }

    [Fact]
    public async Task Deposit_WithFractionOfCent_Returns400WithFieldError()
    {
        Guid accountId = await client.OpenAccountAsync();

        HttpResponseMessage response = await client.PostIdempotentAsync(
            $"/accounts/{accountId}/deposits", new { amount = 10.001m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("Amount", out _));
    }

    [Fact]
    public async Task GetAccount_WhenMissing_Returns404()
    {
        HttpResponseMessage response = await client.GetAsync($"/accounts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Account.NotFound", await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Deposit_WhenAccountIsFrozen_Returns409()
    {
        Guid accountId = await client.OpenAccountAsync();
        await client.PostAsync($"/accounts/{accountId}/freeze", null);

        HttpResponseMessage response = await client.PostIdempotentAsync(
            $"/accounts/{accountId}/deposits", new { amount = 5m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Account.Frozen", await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Transfer_MovesMoneyBetweenAccounts()
    {
        Guid sourceId = await client.OpenAccountAsync("Source");
        Guid destinationId = await client.OpenAccountAsync("Destination");
        await client.DepositAsync(sourceId, 100m);

        HttpResponseMessage response = await client.PostIdempotentAsync("/transfers", new
        {
            sourceAccountId = sourceId,
            destinationAccountId = destinationId,
            amount = 40m
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(60m, await client.GetBalanceAsync(sourceId));
        Assert.Equal(40m, await client.GetBalanceAsync(destinationId));
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return problem.RootElement.GetProperty("code").GetString();
    }
}
