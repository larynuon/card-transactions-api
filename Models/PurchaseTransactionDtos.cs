namespace CardTransactionsApi.Models;

public sealed record CreatePurchaseTransactionRequest(
    Guid CardId,
    string? Description,
    DateTimeOffset? TransactionDate,
    decimal Amount);

public sealed record PurchaseTransactionResponse(
    Guid Id,
    Guid CardId,
    string Description,
    DateTimeOffset TransactionDate,
    decimal Amount,
    DateTimeOffset CreatedAt);

public sealed record TransactionConversionResponse(
    Guid TransactionId,
    string Description,
    DateTimeOffset TransactionDate,
    string SourceCurrency,
    decimal SourceAmount,
    string TargetCurrency,
    decimal ExchangeRate,
    DateOnly RateDate,
    decimal ConvertedAmount);
