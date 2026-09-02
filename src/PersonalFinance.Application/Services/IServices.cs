using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Application.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
    Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
}

public interface IAccountService
{
    Task<Result<PaginatedList<AccountDto>>> GetAccountsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<Result<AccountDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<AccountDto>> CreateAsync(CreateAccountRequest request, CancellationToken ct = default);
    Task<Result<AccountDto>> UpdateAsync(Guid id, UpdateAccountRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface ICategoryService
{
    Task<Result<IReadOnlyList<CategoryDto>>> GetCategoriesAsync(CancellationToken ct = default);
    Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task<Result<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default);
    Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default);
}

public interface ITransactionService
{
    Task<Result<PaginatedList<TransactionDto>>> GetTransactionsAsync(
        Guid? accountId = null,
        Guid? categoryId = null,
        TransactionType? type = null,
        DateTime? from = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);
    Task<Result<TransactionDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<TransactionDto>> CreateAsync(CreateTransactionRequest request, CancellationToken ct = default);
    Task<Result<TransactionDto>> UpdateAsync(Guid id, UpdateTransactionRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface ITransferService
{
    Task<Result<TransferDto>> CreateAsync(CreateTransferRequest request, CancellationToken ct = default);
    Task<Result<PaginatedList<TransferDto>>> GetAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
}

public interface ICreditCardService
{
    Task<Result<IReadOnlyList<CreditCardDto>>> GetCardsAsync(CancellationToken ct = default);
    Task<Result<CreditCardDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<CreditCardDto>> CreateCardAsync(CreateCreditCardRequest request, CancellationToken ct = default);
    Task<Result<CreditCardPurchaseDto>> CreatePurchaseAsync(CreateCreditCardPurchaseRequest request, CancellationToken ct = default);
    Task<Result> MarkPurchasePaidAsync(Guid purchaseId, CancellationToken ct = default);
}

public interface IBudgetService
{
    Task<Result<IReadOnlyList<BudgetDto>>> GetBudgetsAsync(int? month = null, int? year = null, CancellationToken ct = default);
    Task<Result<BudgetDto>> CreateAsync(CreateBudgetRequest request, CancellationToken ct = default);
    Task<Result<BudgetDto>> UpdateLimitAsync(Guid id, decimal limit, CancellationToken ct = default);
}

public interface IGoalService
{
    Task<Result<IReadOnlyList<GoalDto>>> GetGoalsAsync(CancellationToken ct = default);
    Task<Result<GoalDto>> CreateAsync(CreateGoalRequest request, CancellationToken ct = default);
    Task<Result<GoalDto>> ContributeAsync(Guid id, ContributeGoalRequest request, CancellationToken ct = default);
}

public interface IRecurringTransactionService
{
    Task<Result<IReadOnlyList<RecurringTransactionDto>>> GetAsync(CancellationToken ct = default);
    Task<Result<RecurringTransactionDto>> CreateAsync(CreateRecurringTransactionRequest request, CancellationToken ct = default);
    Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default);
    /// <summary>Gera transações para as recorrências vencidas de forma idempotente e segura.</summary>
    Task<Result<int>> ExecuteDueAsync(CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<Result<DashboardDto>> GetDashboardAsync(int? month = null, int? year = null, CancellationToken ct = default);
}

public interface ISyncService
{
    Task<Result<SyncPushResult>> PushAsync(SyncPushRequest request, CancellationToken ct = default);
    Task<Result<SyncPullResponse>> PullAsync(SyncPullRequest request, CancellationToken ct = default);
}
