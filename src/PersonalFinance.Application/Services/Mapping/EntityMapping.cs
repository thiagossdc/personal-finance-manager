using PersonalFinance.Application.Dtos;
using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Application.Services.Mapping;

/// <summary>
/// Mapeamentos manuais entre entidades de domínio e DTOs. Evita assinatura de biblioteca extra
/// e torna as transformações explícitas e testáveis.
/// </summary>
public static class EntityMapping
{
    public static AccountDto ToDto(this Account a) =>
        new(a.Id, a.Name, a.Type, a.InitialBalance.Amount, a.CurrentBalance.Amount, a.Currency, a.IsActive);

    public static CategoryDto ToDto(this Category c) =>
        new(c.Id, c.Name, c.Icon, c.Type, c.ParentId, c.IsActive);

    public static TransactionDto ToDto(this Transaction t, string categoryName) =>
        new(
            t.Id,
            t.AccountId,
            t.CategoryId,
            categoryName,
            t.Type,
            t.Amount.Amount,
            t.Description,
            t.TransactionDate,
            t.Status,
            t.Note,
            t.TransferId,
            t.InstallmentId);

    public static TransferDto ToDto(this Transfer t) =>
        new(t.Id, t.SourceAccountId, t.TargetAccountId, t.Amount.Amount, t.TransferDate, t.Description);

    public static CreditCardDto ToDto(this CreditCard card) =>
        new(
            card.Id,
            card.Name,
            card.LastFourDigits,
            card.CreditLimit.Amount,
            card.ClosingDay,
            card.DueDay,
            card.CurrentOpenAmount().Amount,
            card.AvailableLimit().Amount,
            card.UtilizationRatio(),
            card.Status);

    public static CreditCardPurchaseDto ToDto(this CreditCardTransaction p) =>
        new(p.Id, p.Description, p.Amount.Amount, p.PurchaseDate, p.InstallmentCount, p.Status);

    public static BudgetDto ToDto(this Budget b, string categoryName) =>
        new(
            b.Id,
            b.CategoryId,
            categoryName,
            b.Limit.Amount,
            b.Spent.Amount,
            b.Remaining().Amount,
            b.Progress(),
            b.Period,
            b.Month,
            b.Year,
            b.AlertThreshold);

    public static GoalDto ToDto(this FinancialGoal g) =>
        new(
            g.Id,
            g.Name,
            g.TargetAmount.Amount,
            g.CurrentAmount.Amount,
            g.Remaining().Amount,
            g.Progress(),
            g.Deadline,
            g.Status,
            g.Contributions.Select(c => new GoalContributionDto(c.Amount.Amount, c.Note, c.ContributedAtUtc)).ToList());

    public static RecurringTransactionDto ToDto(this RecurringTransaction r) =>
        new(r.Id, r.AccountId, r.Type, r.Amount.Amount, r.Description, r.Frequency, r.StartDate, r.NextExecution, r.IsActive);
}
