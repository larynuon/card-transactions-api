namespace CardTransactionsApi.Models;

public sealed record CreateCardRequest(decimal CreditLimit);

public sealed record CardResponse(
    Guid Id,
    decimal CreditLimit,
    DateTimeOffset CreatedAt);

public sealed record CardBalanceResponse(
    Guid CardId,
    string SourceCurrency,
    decimal SourceBalance,
    string TargetCurrency,
    decimal ExchangeRate,
    DateOnly RateDate,
    decimal ConvertedBalance);
