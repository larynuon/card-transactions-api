namespace CardTransactionsApi.Entities;

public sealed class PurchaseTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CardId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Card Card { get; set; } = null!;
}
