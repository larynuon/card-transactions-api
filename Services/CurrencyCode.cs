namespace CardTransactionsApi.Services;

public static class CurrencyCode
{
    public static bool TryNormalize(string? value, out string currency)
    {
        currency = value?.Trim().ToUpperInvariant() ?? string.Empty;

        return currency.Length == 3
            && currency.All(character => character is >= 'A' and <= 'Z');
    }
}
