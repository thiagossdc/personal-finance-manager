using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Application.Dtos;

// ---- Auth ----
public sealed record RegisterRequest(string Email, string DisplayName, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record AuthResponse(Guid UserId, string DisplayName, string Email, string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);

// ---- Accounts ----
public sealed record AccountDto(
    Guid Id,
    string Name,
    AccountType Type,
    decimal InitialBalance,
    decimal CurrentBalance,
    string Currency,
    bool IsActive);

public sealed record CreateAccountRequest(string Name, AccountType Type, decimal InitialBalance, string? Currency = null);
public sealed record UpdateAccountRequest(string Name, bool IsActive);

// ---- Categories ----
public sealed record CategoryDto(Guid Id, string Name, string? Icon, TransactionType Type, Guid? ParentId, bool IsActive);
public sealed record CreateCategoryRequest(string Name, TransactionType Type, string? Icon, Guid? ParentId);
public sealed record UpdateCategoryRequest(string? Name, string? Icon, bool? IsActive);

// ---- Transactions ----
public sealed record TransactionDto(
    Guid Id,
    Guid AccountId,
    Guid? CategoryId,
    string CategoryName,
    TransactionType Type,
    decimal Amount,
    string Description,
    DateTime TransactionDate,
    TransactionStatus Status,
    string? Note,
    Guid? TransferId,
    Guid? InstallmentId);

public sealed record CreateTransactionRequest(
    Guid AccountId,
    TransactionType Type,
    decimal Amount,
    string Description,
    DateTime TransactionDate,
    Guid? CategoryId,
    string? Note,
    IReadOnlyList<string>? Tags = null);

public sealed record UpdateTransactionRequest(
    decimal Amount,
    string Description,
    DateTime TransactionDate,
    Guid? CategoryId,
    string? Note);

// ---- Transfers ----
public sealed record TransferDto(Guid Id, Guid SourceAccountId, Guid TargetAccountId, decimal Amount, DateTime TransferDate, string Description);
public sealed record CreateTransferRequest(Guid SourceAccountId, Guid TargetAccountId, decimal Amount, DateTime TransferDate, string? Description = null);

// ---- Credit Cards ----
public sealed record CreditCardDto(
    Guid Id,
    string Name,
    string? LastFourDigits,
    decimal CreditLimit,
    int ClosingDay,
    int DueDay,
    decimal OpenAmount,
    decimal AvailableLimit,
    decimal UtilizationRatio,
    CreditCardStatus Status);

public sealed record CreditCardPurchaseDto(Guid Id, string Description, decimal Amount, DateTime PurchaseDate, int InstallmentCount, PaymentStatus Status);

public sealed record CreateCreditCardRequest(string Name, decimal CreditLimit, int ClosingDay, int DueDay, string? LastFourDigits = null);
public sealed record CreateCreditCardPurchaseRequest(Guid CreditCardId, string Description, decimal Amount, DateTime PurchaseDate, int InstallmentCount = 1, Guid? CategoryId = null);

// ---- Budgets ----
public sealed record BudgetDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    decimal Limit,
    decimal Spent,
    decimal Remaining,
    decimal Progress,
    BudgetPeriod Period,
    int? Month,
    int? Year,
    decimal AlertThreshold);

public sealed record CreateBudgetRequest(Guid CategoryId, decimal Limit, BudgetPeriod Period, int? Month = null, int? Year = null, decimal AlertThreshold = 0.8m);

// ---- Goals ----
public sealed record GoalDto(Guid Id, string Name, decimal TargetAmount, decimal CurrentAmount, decimal Remaining, decimal Progress, DateTime? Deadline, GoalStatus Status, IReadOnlyList<GoalContributionDto>? Contributions = null);
public sealed record GoalContributionDto(decimal Amount, string? Note, DateTime ContributedAtUtc);

public sealed record CreateGoalRequest(string Name, decimal TargetAmount, DateTime? Deadline = null);
public sealed record ContributeGoalRequest(decimal Amount, string? Note = null);

// ---- Recurring ----
public sealed record RecurringTransactionDto(
    Guid Id,
    Guid AccountId,
    TransactionType Type,
    decimal Amount,
    string Description,
    RecurrenceFrequency Frequency,
    DateTime StartDate,
    DateTime? NextExecution,
    bool IsActive);

public sealed record CreateRecurringTransactionRequest(
    Guid AccountId,
    TransactionType Type,
    decimal Amount,
    string Description,
    RecurrenceFrequency Frequency,
    DateTime StartDate,
    Guid? CategoryId = null,
    int Interval = 1,
    DateTime? EndDate = null);
