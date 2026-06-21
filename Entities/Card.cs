namespace CardTransactionsApi.Entities;

public sealed class Card
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal CreditLimit { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<PurchaseTransaction> PurchaseTransactions { get; set; } = [];
}
