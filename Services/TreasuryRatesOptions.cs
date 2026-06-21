namespace CardTransactionsApi.Services;

public sealed class TreasuryRatesOptions
{
    public const string SectionName = "TreasuryRates";

    public string BaseUrl { get; init; } =
        "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/";

    public int TimeoutSeconds { get; init; } = 10;

    public Dictionary<string, string> CurrencyMappings { get; init; } = [];

    public string? GetTreasuryCurrencyDescription(string currency)
    {
        if (CurrencyMappings.TryGetValue(currency, out var description))
        {
            return description;
        }

        return DefaultCurrencyMappings.TryGetValue(currency, out description)
            ? description
            : null;
    }

    private static readonly IReadOnlyDictionary<string, string> DefaultCurrencyMappings =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AUD"] = "Australia-Dollar",
            ["BRL"] = "Brazil-Real",
            ["CAD"] = "Canada-Dollar",
            ["CHF"] = "Switzerland-Franc",
            ["CNY"] = "China-Yuan Renminbi",
            ["DKK"] = "Denmark-Krone",
            ["EUR"] = "Euro Zone-Euro",
            ["GBP"] = "United Kingdom-Pound",
            ["HKD"] = "Hong Kong-Dollar",
            ["INR"] = "India-Rupee",
            ["JPY"] = "Japan-Yen",
            ["KRW"] = "Korea-Won",
            ["MXN"] = "Mexico-Peso",
            ["NOK"] = "Norway-Krone",
            ["NZD"] = "New Zealand-Dollar",
            ["SEK"] = "Sweden-Krona",
            ["SGD"] = "Singapore-Dollar",
            ["ZAR"] = "South Africa-Rand"
        };
}
