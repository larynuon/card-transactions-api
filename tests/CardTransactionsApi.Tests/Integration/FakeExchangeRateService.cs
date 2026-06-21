using CardTransactionsApi.Services;

namespace CardTransactionsApi.Tests.Integration;

public sealed class FakeExchangeRateService : IExchangeRateService
{
    public Task<ExchangeRate> GetLatestUsdExchangeRateAsync(
        string targetCurrency,
        CancellationToken cancellationToken)
    {
        var currency = Normalize(targetCurrency);
        var exchangeRate = new ExchangeRate(
            currency,
            currency == "USD" ? 1m : 2m,
            new DateOnly(2026, 6, 19));

        return Task.FromResult(exchangeRate);
    }

    public Task<ExchangeRate> GetUsdExchangeRateOnOrBeforeAsync(
        string targetCurrency,
        DateOnly transactionDate,
        CancellationToken cancellationToken)
    {
        var currency = Normalize(targetCurrency);

        if (currency == "CAD")
        {
            throw new CurrencyConversionUnavailableException(
                currency,
                $"The transaction cannot be converted to {currency} because no Treasury exchange rate is available within 6 months on or before {transactionDate:yyyy-MM-dd}.");
        }

        var exchangeRate = new ExchangeRate(
            currency,
            currency == "USD" ? 1m : 3m,
            currency == "USD" ? transactionDate : transactionDate.AddDays(-1));

        return Task.FromResult(exchangeRate);
    }

    private static string Normalize(string targetCurrency)
    {
        if (!CurrencyCode.TryNormalize(targetCurrency, out var currency))
        {
            throw new UnsupportedCurrencyException(targetCurrency, "Invalid currency.");
        }

        return currency;
    }
}
