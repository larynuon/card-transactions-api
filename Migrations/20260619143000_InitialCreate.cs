using System;
using CardTransactionsApi.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardTransactionsApi.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260619143000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Cards",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreditLimit = table.Column<decimal>(
                    type: "numeric(18,2)",
                    precision: 18,
                    scale: 2,
                    nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false,
                    defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Cards", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PurchaseTransactions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CardId = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(
                    type: "character varying(200)",
                    maxLength: 200,
                    nullable: false),
                TransactionDate = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                Amount = table.Column<decimal>(
                    type: "numeric(18,2)",
                    precision: 18,
                    scale: 2,
                    nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false,
                    defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PurchaseTransactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_PurchaseTransactions_Cards_CardId",
                    column: x => x.CardId,
                    principalTable: "Cards",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PurchaseTransactions_CardId",
            table: "PurchaseTransactions",
            column: "CardId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PurchaseTransactions");
        migrationBuilder.DropTable(name: "Cards");
    }
}
