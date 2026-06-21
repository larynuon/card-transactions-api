using CardTransactionsApi.Data;
using CardTransactionsApi.Entities;
using CardTransactionsApi.Models;
using CardTransactionsApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CardTransactionsApi.Controllers;

[ApiController]
[Route("api/cards")]
public sealed class CardsController(
    AppDbContext dbContext,
    IExchangeRateService exchangeRateService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CardResponse>> CreateCard(
        CreateCardRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CreditLimit <= 0)
        {
            return ValidationError(nameof(request.CreditLimit), "Credit limit must be greater than zero.");
        }

        var card = new Card
        {
            CreditLimit = RoundMoney(request.CreditLimit)
        };

        dbContext.Cards.Add(card);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCard), new { id = card.Id }, ToResponse(card));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CardResponse>> GetCard(Guid id, CancellationToken cancellationToken)
    {
        var card = await dbContext.Cards.FindAsync([id], cancellationToken);
        return card is null ? NotFound() : Ok(ToResponse(card));
    }

    [HttpGet("{id:guid}/balance")]
    public async Task<ActionResult<CardBalanceResponse>> GetBalance(
        Guid id,
        [FromQuery] string currency = "AUD",
        CancellationToken cancellationToken = default)
    {
        if (!CurrencyCode.TryNormalize(currency, out var targetCurrency))
        {
            return ValidationError(nameof(currency), "Currency must be a three-letter ISO currency code.");
        }

        var card = await dbContext.Cards
            .AsNoTracking()
            .SingleOrDefaultAsync(current => current.Id == id, cancellationToken);

        if (card is null)
        {
            return NotFound();
        }

        var spent = await dbContext.PurchaseTransactions
            .Where(transaction => transaction.CardId == id)
            .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken) ?? 0m;

        var sourceBalance = RoundMoney(card.CreditLimit - spent);

        try
        {
            var rate = await exchangeRateService.GetLatestUsdExchangeRateAsync(targetCurrency, cancellationToken);
            return Ok(new CardBalanceResponse(
                card.Id,
                "USD",
                sourceBalance,
                rate.TargetCurrency,
                rate.Rate,
                rate.RateDate,
                ConvertMoney(sourceBalance, rate.Rate)));
        }
        catch (UnsupportedCurrencyException exception)
        {
            return ValidationError(nameof(currency), exception.Message);
        }
        catch (ExchangeRateUnavailableException exception)
        {
            return Problem(exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static CardResponse ToResponse(Card card) =>
        new(card.Id, card.CreditLimit, card.CreatedAt);

    private static decimal RoundMoney(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    private static decimal ConvertMoney(decimal amount, decimal exchangeRate) =>
        RoundMoney(amount * exchangeRate);

    private ActionResult ValidationError(string field, string message) =>
        BadRequest(new ValidationProblemDetails(
            new Dictionary<string, string[]> { [field] = [message] })
        {
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest
        });
}
