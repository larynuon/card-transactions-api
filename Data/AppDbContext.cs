using CardTransactionsApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace CardTransactionsApi.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<PurchaseTransaction> PurchaseTransactions => Set<PurchaseTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Card>(entity =>
        {
            entity.HasKey(card => card.Id);
            entity.Property(card => card.CreditLimit).HasPrecision(18, 2);
            entity.Property(card => card.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            entity.HasMany(card => card.PurchaseTransactions)
                .WithOne(transaction => transaction.Card)
                .HasForeignKey(transaction => transaction.CardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PurchaseTransaction>(entity =>
        {
            entity.HasKey(transaction => transaction.Id);
            entity.Property(transaction => transaction.Description)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(transaction => transaction.TransactionDate)
                .HasColumnType("timestamp with time zone");
            entity.Property(transaction => transaction.Amount).HasPrecision(18, 2);
            entity.Property(transaction => transaction.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            entity.HasIndex(transaction => transaction.CardId);
        });
    }
}
