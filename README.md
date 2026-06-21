# Card Transactions API

Production-style ASP.NET Core Web API for cards, purchase transactions, and USD-to-currency conversion using the Treasury Reporting Rates of Exchange API.

Amounts are stored in USD. Converted transaction amounts and card balances are returned in the requested three-letter currency code. Transaction conversion uses a Treasury exchange rate dated on or before the purchase date within the prior 6 months. Balance conversion uses the latest available Treasury exchange rate.

## Tech Stack

- .NET 8
- ASP.NET Core Web API
- PostgreSQL
- Entity Framework Core
- Docker Compose
- xUnit functional/integration tests

## Configuration

The app reads configuration from `appsettings.json`, `appsettings.Development.json`, and environment variables. Use double underscores for environment variable overrides.

Common variables:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=card_transactions;Username=postgres;Password=postgres"
$env:TreasuryRates__BaseUrl = "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/"
$env:TreasuryRates__TimeoutSeconds = "10"
```

To add or fix a Treasury currency mapping:

```powershell
$env:TreasuryRates__CurrencyMappings__AUD = "Australia-Dollar"
```

## Run Locally

Start PostgreSQL:

```powershell
docker compose up -d postgres
```

Restore packages:

```powershell
dotnet restore tests/CardTransactionsApi.Tests/CardTransactionsApi.Tests.csproj
```

Apply EF Core migrations:

```powershell
dotnet tool install --global dotnet-ef --version 8.*
dotnet ef database update --project card-transactions-api.csproj
```

Run the API:

```powershell
dotnet run --project card-transactions-api.csproj
```

## Run With Docker Compose

Build and run the API plus PostgreSQL:

```powershell
docker compose up --build
```

The API listens on:

```text
http://localhost:8080
```

Stop the containers:

```powershell
docker compose down
```

Remove the database volume:

```powershell
docker compose down -v
```

## Test

Run all tests:

```powershell
dotnet test tests/CardTransactionsApi.Tests/CardTransactionsApi.Tests.csproj
```

The integration tests use `WebApplicationFactory`, an in-memory EF Core database, and a fake exchange-rate service so they do not depend on PostgreSQL or the external Treasury API.

## Endpoints

Create a card:

```http
POST /api/cards
Content-Type: application/json

{
  "creditLimit": 1500
}
```

Create a purchase transaction:

```http
POST /api/purchase-transactions
Content-Type: application/json

{
  "cardId": "00000000-0000-0000-0000-000000000000",
  "description": "Laptop stand",
  "transactionDate": "2026-06-19T10:30:00Z",
  "amount": 89.95
}
```

Retrieve a converted purchase transaction:

```http
GET /api/purchase-transactions/{id}/converted?currency=AUD
```

The converted transaction response includes the transaction identifier, description, transaction date, original USD amount, exchange rate, rate date, and converted amount. If no exchange rate is available within 6 months on or before the transaction date, the API returns `422 Unprocessable Entity`.

Retrieve converted available card balance:

```http
GET /api/cards/{id}/balance?currency=AUD
```

## Notes

- Purchase transactions are rejected when they would exceed the card credit limit.
- `USD` uses an exchange rate of `1`.
- Transaction conversion uses the latest Treasury rate on or before the transaction date, but not older than 6 months.
- Balance conversion uses the latest Treasury rate for the requested currency.
- Other currencies must be mapped to a Treasury `country_currency_desc` value in `TreasuryRates:CurrencyMappings`.
