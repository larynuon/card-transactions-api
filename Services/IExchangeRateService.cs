namespace CardTransactionsApi.Services;

public interface IExchangeRateService
{
    Task<ExchangeRate> GetLatestUsdExchangeRateAsync(
        string targetCurrency,
        CancellationToken cancellationToken);

    Task<ExchangeRate> GetUsdExchangeRateOnOrBeforeAsync(
        string targetCurrency,
        DateOnly transactionDate,
        CancellationToken cancellationToken);
}

public sealed record ExchangeRate(
    string TargetCurrency,
    decimal Rate,
    DateOnly RateDate);
