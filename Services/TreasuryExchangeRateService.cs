using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace CardTransactionsApi.Services;

public sealed class TreasuryExchangeRateService(
    HttpClient httpClient,
    IOptions<TreasuryRatesOptions> options) : IExchangeRateService
{
    public Task<ExchangeRate> GetLatestUsdExchangeRateAsync(
        string targetCurrency,
        CancellationToken cancellationToken)
    {
        return GetUsdExchangeRateAsync(
            targetCurrency,
            noDataMessage: currency => $"No Treasury exchange rate was returned for '{currency}'.",
            noDataMeansConversionUnavailable: false,
            cancellationToken: cancellationToken);
    }

    public Task<ExchangeRate> GetUsdExchangeRateOnOrBeforeAsync(
        string targetCurrency,
        DateOnly transactionDate,
        CancellationToken cancellationToken)
    {
        var earliestRateDate = transactionDate.AddMonths(-6);

        return GetUsdExchangeRateAsync(
            targetCurrency,
            noDataMessage: currency =>
                $"The transaction cannot be converted to {currency} because no Treasury exchange rate is available within 6 months on or before {transactionDate:yyyy-MM-dd}.",
            noDataMeansConversionUnavailable: true,
            cancellationToken: cancellationToken,
            earliestRateDate: earliestRateDate,
            latestRateDate: transactionDate);
    }

    private async Task<ExchangeRate> GetUsdExchangeRateAsync(
        string targetCurrency,
        Func<string, string> noDataMessage,
        bool noDataMeansConversionUnavailable,
        CancellationToken cancellationToken,
        DateOnly? earliestRateDate = null,
        DateOnly? latestRateDate = null)
    {
        if (!CurrencyCode.TryNormalize(targetCurrency, out var currency))
        {
            throw new UnsupportedCurrencyException(
                targetCurrency,
                "Currency must be a three-letter ISO currency code.");
        }

        if (currency == "USD")
        {
            return new ExchangeRate(
                currency,
                1m,
                latestRateDate ?? DateOnly.FromDateTime(DateTime.UtcNow));
        }

        var treasuryCurrency = options.Value.GetTreasuryCurrencyDescription(currency);
        if (treasuryCurrency is null)
        {
            throw new UnsupportedCurrencyException(
                currency,
                $"Currency '{currency}' is not mapped to a Treasury Reporting Rates currency.");
        }

        var filters = new List<string>
        {
            $"country_currency_desc:eq:{treasuryCurrency}"
        };

        if (earliestRateDate is not null)
        {
            filters.Add($"record_date:gte:{earliestRateDate.Value:yyyy-MM-dd}");
        }

        if (latestRateDate is not null)
        {
            filters.Add($"record_date:lte:{latestRateDate.Value:yyyy-MM-dd}");
        }

        var requestUri = QueryHelpers.AddQueryString(
            "rates_of_exchange",
            new Dictionary<string, string?>
            {
                ["fields"] = "record_date,country_currency_desc,exchange_rate",
                ["filter"] = string.Join(",", filters),
                ["sort"] = "-record_date",
                ["page[size]"] = "1"
            });

        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ExchangeRateUnavailableException(
                currency,
                $"Treasury exchange rate request failed with status code {(int)response.StatusCode}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<TreasuryRatesResponse>(
            cancellationToken: cancellationToken);

        var record = payload?.Data.FirstOrDefault();
        if (record is null)
        {
            if (noDataMeansConversionUnavailable)
            {
                throw new CurrencyConversionUnavailableException(
                    currency,
                    noDataMessage(currency));
            }

            throw new ExchangeRateUnavailableException(currency, noDataMessage(currency));
        }

        if (!decimal.TryParse(
                record.ExchangeRate,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var rate))
        {
            throw new ExchangeRateUnavailableException(
                currency,
                $"Treasury exchange rate for '{currency}' was not a valid decimal value.");
        }

        if (!DateOnly.TryParse(
                record.RecordDate,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var rateDate))
        {
            throw new ExchangeRateUnavailableException(
                currency,
                $"Treasury exchange rate for '{currency}' did not include a valid record date.");
        }

        if (earliestRateDate is not null && rateDate < earliestRateDate.Value ||
            latestRateDate is not null && rateDate > latestRateDate.Value)
        {
            throw new CurrencyConversionUnavailableException(
                currency,
                noDataMessage(currency));
        }

        return new ExchangeRate(currency, rate, rateDate);
    }

    private sealed class TreasuryRatesResponse
    {
        [JsonPropertyName("data")]
        public List<TreasuryRateRecord> Data { get; init; } = [];
    }

    private sealed class TreasuryRateRecord
    {
        [JsonPropertyName("record_date")]
        public string? RecordDate { get; init; }

        [JsonPropertyName("exchange_rate")]
        public string? ExchangeRate { get; init; }
    }
}
