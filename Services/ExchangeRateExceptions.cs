namespace CardTransactionsApi.Services;

public sealed class UnsupportedCurrencyException(string currency, string message) : Exception(message)
{
    public string Currency { get; } = currency;
}

public sealed class ExchangeRateUnavailableException(string currency, string message) : Exception(message)
{
    public string Currency { get; } = currency;
}

public sealed class CurrencyConversionUnavailableException(string currency, string message) : Exception(message)
{
    public string Currency { get; } = currency;
}
