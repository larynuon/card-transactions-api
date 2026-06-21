using CardTransactionsApi.Data;
using CardTransactionsApi.Entities;
using CardTransactionsApi.Models;
using CardTransactionsApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CardTransactionsApi.Controllers;

[ApiController]
[Route("api/purchase-transactions")]
public sealed class PurchaseTransactionsController(
    AppDbContext dbContext,
    IExchangeRateService exchangeRateService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PurchaseTransactionResponse>> CreatePurchaseTransaction(
        CreatePurchaseTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return ValidationErrors(validationErrors);
        }

        var card = await dbContext.Cards
            .SingleOrDefaultAsync(current => current.Id == request.CardId, cancellationToken);

        if (card is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Card not found.",
                Detail = $"Card '{request.CardId}' does not exist.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var spent = await dbContext.PurchaseTransactions
            .Where(transaction => transaction.CardId == request.CardId)
            .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken) ?? 0m;

        var amount = RoundMoney(request.Amount);
        if (spent + amount > card.CreditLimit)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Credit limit exceeded.",
                Detail = "The purchase transaction would exceed the card credit limit.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var transaction = new PurchaseTransaction
        {
            CardId = request.CardId,
            Description = request.Description!.Trim(),
            TransactionDate = request.TransactionDate!.Value.ToUniversalTime(),
            Amount = amount
        };

        dbContext.PurchaseTransactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetConvertedTransaction),
            new { id = transaction.Id, currency = "AUD" },
            ToResponse(transaction));
    }

    [HttpGet("{id:guid}/converted")]
    public async Task<ActionResult<TransactionConversionResponse>> GetConvertedTransaction(
        Guid id,
        [FromQuery] string currency = "AUD",
        CancellationToken cancellationToken = default)
    {
        if (!CurrencyCode.TryNormalize(currency, out var targetCurrency))
        {
            return ValidationError(nameof(currency), "Currency must be a three-letter ISO currency code.");
        }

        var transaction = await dbContext.PurchaseTransactions
            .AsNoTracking()
            .SingleOrDefaultAsync(current => current.Id == id, cancellationToken);

        if (transaction is null)
        {
            return NotFound();
        }

        try
        {
            var transactionDate = DateOnly.FromDateTime(transaction.TransactionDate.UtcDateTime);
            var rate = await exchangeRateService.GetUsdExchangeRateOnOrBeforeAsync(
                targetCurrency,
                transactionDate,
                cancellationToken);

            return Ok(new TransactionConversionResponse(
                transaction.Id,
                transaction.Description,
                transaction.TransactionDate,
                "USD",
                transaction.Amount,
                rate.TargetCurrency,
                rate.Rate,
                rate.RateDate,
                ConvertMoney(transaction.Amount, rate.Rate)));
        }
        catch (UnsupportedCurrencyException exception)
        {
            return ValidationError(nameof(currency), exception.Message);
        }
        catch (ExchangeRateUnavailableException exception)
        {
            return Problem(exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (CurrencyConversionUnavailableException exception)
        {
            return Problem(exception.Message, statusCode: StatusCodes.Status422UnprocessableEntity);
        }
    }

    private static Dictionary<string, string[]> Validate(CreatePurchaseTransactionRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.CardId == Guid.Empty)
        {
            errors[nameof(request.CardId)] = ["Card id is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors[nameof(request.Description)] = ["Description is required."];
        }
        else if (request.Description.Length > 200)
        {
            errors[nameof(request.Description)] = ["Description cannot exceed 200 characters."];
        }

        if (request.TransactionDate is null)
        {
            errors[nameof(request.TransactionDate)] = ["Transaction date is required."];
        }

        if (request.Amount <= 0)
        {
            errors[nameof(request.Amount)] = ["Amount must be greater than zero."];
        }

        return errors;
    }

    private static PurchaseTransactionResponse ToResponse(PurchaseTransaction transaction) =>
        new(
            transaction.Id,
            transaction.CardId,
            transaction.Description,
            transaction.TransactionDate,
            transaction.Amount,
            transaction.CreatedAt);

    private static decimal RoundMoney(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    private static decimal ConvertMoney(decimal amount, decimal exchangeRate) =>
        RoundMoney(amount * exchangeRate);

    private ActionResult ValidationError(string field, string message) =>
        ValidationErrors(new Dictionary<string, string[]> { [field] = [message] });

    private ActionResult ValidationErrors(Dictionary<string, string[]> errors) =>
        BadRequest(new ValidationProblemDetails(errors)
        {
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest
        });
}
