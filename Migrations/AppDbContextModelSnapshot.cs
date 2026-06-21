using System;
using CardTransactionsApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace CardTransactionsApi.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "8.0.4");

        modelBuilder.Entity("CardTransactionsApi.Entities.Card", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<DateTimeOffset>("CreatedAt")
                .ValueGeneratedOnAdd()
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            b.Property<decimal>("CreditLimit")
                .HasPrecision(18, 2)
                .HasColumnType("numeric(18,2)");

            b.HasKey("Id");

            b.ToTable("Cards");
        });

        modelBuilder.Entity("CardTransactionsApi.Entities.PurchaseTransaction", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<decimal>("Amount")
                .HasPrecision(18, 2)
                .HasColumnType("numeric(18,2)");

            b.Property<Guid>("CardId")
                .HasColumnType("uuid");

            b.Property<DateTimeOffset>("CreatedAt")
                .ValueGeneratedOnAdd()
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            b.Property<string>("Description")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            b.Property<DateTimeOffset>("TransactionDate")
                .HasColumnType("timestamp with time zone");

            b.HasKey("Id");

            b.HasIndex("CardId");

            b.ToTable("PurchaseTransactions");
        });

        modelBuilder.Entity("CardTransactionsApi.Entities.PurchaseTransaction", b =>
        {
            b.HasOne("CardTransactionsApi.Entities.Card", "Card")
                .WithMany("PurchaseTransactions")
                .HasForeignKey("CardId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Card");
        });

        modelBuilder.Entity("CardTransactionsApi.Entities.Card", b =>
        {
            b.Navigation("PurchaseTransactions");
        });
    }
}
