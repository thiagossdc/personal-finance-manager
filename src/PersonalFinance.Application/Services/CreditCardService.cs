using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services.Mapping;
using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Application.Services;

public sealed class CreditCardService : ServiceBase, ICreditCardService
{
    public CreditCardService(IAppDbContext db, ICurrentUser currentUser, IDateTime time)
        : base(db, currentUser, time)
    {
    }

    private IQueryable<CreditCard> ForUserCards(Guid userId) =>
        Db.CreditCards
            .Where(c => c.UserId == userId)
            .Include(c => c.Transactions)
                .ThenInclude(t => t.Installments);

    public async Task<Result<IReadOnlyList<CreditCardDto>>> GetCardsAsync(CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var cards = await ForUserCards(userId).OrderBy(c => c.Name).ToListAsync(ct);
        return Result<IReadOnlyList<CreditCardDto>>.Success(cards.Select(c => c.ToDto()).ToList());
    }

    public async Task<Result<CreditCardDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var card = await ForUserCards(userId).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (card is null)
        {
            return Error.NotFound("Credit card was not found.");
        }

        return Result<CreditCardDto>.Success(card.ToDto());
    }

    public async Task<Result<CreditCardDto>> CreateCardAsync(CreateCreditCardRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var createResult = CreditCard.Create(userId, request.Name, Money.From(request.CreditLimit), request.ClosingDay, request.DueDay, request.LastFourDigits);
        if (createResult.IsFailure)
        {
            return createResult.Error!;
        }

        Db.CreditCards.Add(createResult.Value!);
        await Db.SaveChangesAsync(ct);
        return Result<CreditCardDto>.Success(createResult.Value!.ToDto());
    }

    public async Task<Result<CreditCardPurchaseDto>> CreatePurchaseAsync(CreateCreditCardPurchaseRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var card = await ForUserCards(userId).FirstOrDefaultAsync(c => c.Id == request.CreditCardId, ct);
        if (card is null)
        {
            return Error.NotFound("Credit card was not found.");
        }

        var purchaseResult = CreditCardTransaction.Create(
            userId, card.Id, request.Description, Money.From(request.Amount, card.CreditLimit.Currency),
            request.PurchaseDate, request.InstallmentCount, request.CategoryId);
        if (purchaseResult.IsFailure)
        {
            return purchaseResult.Error!;
        }

        var purchase = purchaseResult.Value!;

        if (request.InstallmentCount > 1)
        {
            GenerateInstallments(purchase, request.PurchaseDate);
        }

        card.AddPurchase(purchase);
        await Db.SaveChangesAsync(ct);
        return Result<CreditCardPurchaseDto>.Success(purchase.ToDto());
    }

    /// <summary>Divide o total em parcelas mensais, ajustando a última para compensar arredondamento.</summary>
    private static void GenerateInstallments(CreditCardTransaction purchase, DateTime purchaseDate)
    {
        var total = purchase.Amount.Amount;
        var count = purchase.InstallmentCount;
        var perInstallment = decimal.Round(total / count, 2, MidpointRounding.AwayFromZero);
        var due = new DateTime(purchaseDate.Year, purchaseDate.Month, 1).AddMonths(1);

        for (var i = 1; i <= count; i++)
        {
            var amount = i == count
                ? total - (perInstallment * (count - 1))
                : perInstallment;
            var installment = Installment.Create(
                purchase.UserId, purchase.Id, i, count, Money.From(amount, purchase.Amount.Currency), due).Value!;
            purchase.AddInstallment(installment);
            due = due.AddMonths(1);
        }
    }

    public async Task<Result> MarkPurchasePaidAsync(Guid purchaseId, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var purchase = await Db.CreditCardTransactions
            .Include(t => t.Installments)
            .FirstOrDefaultAsync(t => t.Id == purchaseId && t.UserId == userId, ct);
        if (purchase is null)
        {
            return Error.NotFound("Purchase was not found.");
        }

        purchase.MarkPaid();
        await Db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
