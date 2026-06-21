using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CardTransactionsApi.Models;
using Xunit;

namespace CardTransactionsApi.Tests.Integration;

public sealed class CardTransactionsTests : IClassFixture<CardTransactionsApiFactory>
{
    private readonly HttpClient _client;

    public CardTransactionsTests(CardTransactionsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateCardAndRetrieveConvertedBalance()
    {
        var card = await CreateCardAsync(1000m);
        await CreateTransactionAsync(card.Id, 125.25m);

        var balance = await _client.GetFromJsonAsync<CardBalanceResponse>(
            $"/api/cards/{card.Id}/balance?currency=AUD");

        Assert.NotNull(balance);
        Assert.Equal(card.Id, balance.CardId);
        Assert.Equal("USD", balance.SourceCurrency);
        Assert.Equal(874.75m, balance.SourceBalance);
        Assert.Equal("AUD", balance.TargetCurrency);
        Assert.Equal(2m, balance.ExchangeRate);
        Assert.Equal(1749.50m, balance.ConvertedBalance);
    }

    [Fact]
    public async Task CreateTransactionAndRetrieveConvertedTransaction()
    {
        var card = await CreateCardAsync(500m);
        var transactionDate = new DateTimeOffset(2026, 6, 19, 10, 30, 0, TimeSpan.Zero);
        var transaction = await CreateTransactionAsync(
            card.Id,
            42.50m,
            "Coffee",
            transactionDate);

        var conversion = await _client.GetFromJsonAsync<TransactionConversionResponse>(
            $"/api/purchase-transactions/{transaction.Id}/converted?currency=EUR");

        Assert.NotNull(conversion);
        Assert.Equal(transaction.Id, conversion.TransactionId);
        Assert.Equal("Coffee", conversion.Description);
        Assert.Equal(transactionDate, conversion.TransactionDate);
        Assert.Equal("USD", conversion.SourceCurrency);
        Assert.Equal(42.50m, conversion.SourceAmount);
        Assert.Equal("EUR", conversion.TargetCurrency);
        Assert.Equal(3m, conversion.ExchangeRate);
        Assert.Equal(new DateOnly(2026, 6, 18), conversion.RateDate);
        Assert.Equal(127.50m, conversion.ConvertedAmount);
    }

    [Fact]
    public async Task ConvertedTransactionUsesTransactionDateBoundedRate()
    {
        var card = await CreateCardAsync(500m);
        var transaction = await CreateTransactionAsync(
            card.Id,
            10m,
            transactionDate: new DateTimeOffset(2024, 1, 15, 8, 0, 0, TimeSpan.Zero));

        var conversion = await _client.GetFromJsonAsync<TransactionConversionResponse>(
            $"/api/purchase-transactions/{transaction.Id}/converted?currency=AUD");

        Assert.NotNull(conversion);
        Assert.Equal(new DateOnly(2024, 1, 14), conversion.RateDate);
        Assert.Equal(30m, conversion.ConvertedAmount);
    }

    [Fact]
    public async Task ReturnsErrorWhenNoTransactionRateExistsWithinSixMonths()
    {
        var card = await CreateCardAsync(500m);
        var transaction = await CreateTransactionAsync(
            card.Id,
            10m,
            transactionDate: new DateTimeOffset(2024, 1, 15, 8, 0, 0, TimeSpan.Zero));

        using var response = await _client.GetAsync(
            $"/api/purchase-transactions/{transaction.Id}/converted?currency=CAD");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var problem = await ReadJsonAsync<ProblemDetailsResponse>(response);
        Assert.Contains("cannot be converted to CAD", problem.Detail);
    }

    [Fact]
    public async Task RejectsTransactionThatWouldExceedCreditLimit()
    {
        var card = await CreateCardAsync(100m);
        await CreateTransactionAsync(card.Id, 75m);

        using var response = await _client.PostAsJsonAsync(
            "/api/purchase-transactions",
            new
            {
                cardId = card.Id,
                description = "Too much",
                transactionDate = DateTimeOffset.UtcNow,
                amount = 30m
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RejectsInvalidCurrencyCode()
    {
        var card = await CreateCardAsync(250m);

        using var response = await _client.GetAsync($"/api/cards/{card.Id}/balance?currency=AU");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<CardResponse> CreateCardAsync(decimal creditLimit)
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/cards",
            new { creditLimit });

        response.EnsureSuccessStatusCode();

        return await ReadJsonAsync<CardResponse>(response);
    }

    private async Task<PurchaseTransactionResponse> CreateTransactionAsync(
        Guid cardId,
        decimal amount,
        string description = "Coffee",
        DateTimeOffset? transactionDate = null)
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/purchase-transactions",
            new
            {
                cardId,
                description,
                transactionDate = transactionDate ?? DateTimeOffset.UtcNow,
                amount
            });

        response.EnsureSuccessStatusCode();

        return await ReadJsonAsync<PurchaseTransactionResponse>(response);
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        var value = await JsonSerializer.DeserializeAsync<T>(
            stream,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(value);
        return value;
    }

    private sealed record ProblemDetailsResponse(string? Detail);
}
