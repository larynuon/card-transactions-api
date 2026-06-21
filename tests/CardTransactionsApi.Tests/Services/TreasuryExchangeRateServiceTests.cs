using System.Net;
using System.Text;
using CardTransactionsApi.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Xunit;

namespace CardTransactionsApi.Tests.Services;

public sealed class TreasuryExchangeRateServiceTests
{
    [Fact]
    public async Task DateBoundedLookupQueriesRatesOnOrBeforeTransactionDateWithinSixMonths()
    {
        Uri? requestedUri = null;
        var service = CreateService(request =>
        {
            requestedUri = request.RequestUri;

            return JsonResponse("""
                {
                  "data": [
                    {
                      "record_date": "2026-06-18",
                      "exchange_rate": "1.5000"
                    }
                  ]
                }
                """);
        });

        var rate = await service.GetUsdExchangeRateOnOrBeforeAsync(
            "AUD",
            new DateOnly(2026, 6, 19),
            CancellationToken.None);

        Assert.Equal("AUD", rate.TargetCurrency);
        Assert.Equal(1.5m, rate.Rate);
        Assert.Equal(new DateOnly(2026, 6, 18), rate.RateDate);

        Assert.NotNull(requestedUri);
        var query = QueryHelpers.ParseQuery(requestedUri.Query);
        Assert.Equal("record_date,country_currency_desc,exchange_rate", query["fields"].ToString());
        Assert.Equal("-record_date", query["sort"].ToString());
        Assert.Equal("1", query["page[size]"].ToString());

        var filter = query["filter"].ToString();
        Assert.Contains("country_currency_desc:eq:Australia-Dollar", filter);
        Assert.Contains("record_date:gte:2025-12-19", filter);
        Assert.Contains("record_date:lte:2026-06-19", filter);
    }

    [Fact]
    public async Task DateBoundedLookupThrowsConversionErrorWhenNoRateExists()
    {
        var service = CreateService(_ => JsonResponse("""{"data": []}"""));

        var exception = await Assert.ThrowsAsync<CurrencyConversionUnavailableException>(() =>
            service.GetUsdExchangeRateOnOrBeforeAsync(
                "AUD",
                new DateOnly(2026, 6, 19),
                CancellationToken.None));

        Assert.Contains("transaction cannot be converted to AUD", exception.Message);
        Assert.Contains("2026-06-19", exception.Message);
    }

    [Fact]
    public async Task DateBoundedLookupRejectsRateOutsideSixMonthWindowDefensively()
    {
        var service = CreateService(_ => JsonResponse("""
            {
              "data": [
                {
                  "record_date": "2025-12-18",
                  "exchange_rate": "1.5000"
                }
              ]
            }
            """));

        await Assert.ThrowsAsync<CurrencyConversionUnavailableException>(() =>
            service.GetUsdExchangeRateOnOrBeforeAsync(
                "AUD",
                new DateOnly(2026, 6, 19),
                CancellationToken.None));
    }

    [Fact]
    public async Task LatestLookupThrowsServiceErrorWhenNoRateExists()
    {
        var service = CreateService(_ => JsonResponse("""{"data": []}"""));

        await Assert.ThrowsAsync<ExchangeRateUnavailableException>(() =>
            service.GetLatestUsdExchangeRateAsync("AUD", CancellationToken.None));
    }

    [Fact]
    public async Task UsdDoesNotCallTreasury()
    {
        var calls = 0;
        var service = CreateService(_ =>
        {
            calls++;
            return JsonResponse("""{"data": []}""");
        });

        var transactionRate = await service.GetUsdExchangeRateOnOrBeforeAsync(
            "usd",
            new DateOnly(2026, 6, 19),
            CancellationToken.None);
        var latestRate = await service.GetLatestUsdExchangeRateAsync("USD", CancellationToken.None);

        Assert.Equal(0, calls);
        Assert.Equal(1m, transactionRate.Rate);
        Assert.Equal(new DateOnly(2026, 6, 19), transactionRate.RateDate);
        Assert.Equal(1m, latestRate.Rate);
    }

    private static TreasuryExchangeRateService CreateService(
        Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("https://example.test/")
        };

        var options = Options.Create(new TreasuryRatesOptions
        {
            CurrencyMappings = new Dictionary<string, string>
            {
                ["AUD"] = "Australia-Dollar"
            }
        });

        return new TreasuryExchangeRateService(httpClient, options);
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
        }
    }
}
