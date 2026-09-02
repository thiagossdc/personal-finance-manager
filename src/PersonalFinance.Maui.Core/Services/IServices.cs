namespace PersonalFinance.Maui.Core.Services;

public interface ITokenStorage
{
    Task SaveTokensAsync(string accessToken, string refreshToken);
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task ClearAsync();
}

public interface IConnectivityService
{
    bool IsConnected { get; }
    event EventHandler<bool>? ConnectivityChanged;
}

public interface IAuthApiClient
{
    Task<AuthTokenResult?> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<AuthTokenResult?> RegisterAsync(string email, string displayName, string password, CancellationToken ct = default);
}

public sealed record AuthTokenResult(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);

public interface ILocalFinanceService
{
    // Contas
    Task<List<Data.Entities.LocalAccount>> GetAccountsAsync(CancellationToken ct = default);
    Task<Data.Entities.LocalAccount?> GetAccountAsync(string id, CancellationToken ct = default);
    Task<Data.Entities.LocalAccount> CreateAccountAsync(string name, string type, decimal initialBalance, string currency = "BRL", CancellationToken ct = default);
    Task UpdateAccountAsync(string id, string name, bool isActive, CancellationToken ct = default);
    Task DeleteAccountAsync(string id, CancellationToken ct = default);

    // Categorias
    Task<List<Data.Entities.LocalCategory>> GetCategoriesAsync(CancellationToken ct = default);
    Task<Data.Entities.LocalCategory> CreateCategoryAsync(string name, string type, string? icon = null, string? parentId = null, CancellationToken ct = default);
    Task UpdateCategoryAsync(string id, string name, string? icon, bool isActive, CancellationToken ct = default);
    Task DeleteCategoryAsync(string id, CancellationToken ct = default);

    // Transações
    Task<List<Data.Entities.LocalTransaction>> GetTransactionsAsync(int limit = 100, CancellationToken ct = default);
    Task<Data.Entities.LocalTransaction> CreateTransactionAsync(string accountId, string type, decimal amount, string description, DateTime transactionDate, string? categoryId = null, string? note = null, CancellationToken ct = default);
    Task UpdateTransactionAsync(string id, decimal amount, string description, DateTime transactionDate, string? categoryId = null, string? note = null, CancellationToken ct = default);
    Task DeleteTransactionAsync(string id, CancellationToken ct = default);

    // Transferências
    Task<List<Data.Entities.LocalTransfer>> GetTransfersAsync(CancellationToken ct = default);
    Task<Data.Entities.LocalTransfer> CreateTransferAsync(string sourceAccountId, string targetAccountId, decimal amount, string description, DateTime? transferDate = null, CancellationToken ct = default);

    // Cartões de Crédito
    Task<List<Data.Entities.LocalCreditCard>> GetCreditCardsAsync(CancellationToken ct = default);
    Task<Data.Entities.LocalCreditCard> CreateCreditCardAsync(string name, decimal creditLimit, int closingDay, int dueDay, string? lastFourDigits = null, CancellationToken ct = default);
    Task RecordCreditCardPurchaseAsync(string creditCardId, string description, decimal amount, int installmentCount = 1, string? categoryId = null, CancellationToken ct = default);
    Task PayCreditCardAsync(string creditCardId, string sourceAccountId, decimal amount, CancellationToken ct = default);

    // Orçamentos
    Task<List<LocalBudgetWithProgress>> GetBudgetsAsync(int? month = null, int? year = null, CancellationToken ct = default);
    Task<Data.Entities.LocalBudget> CreateBudgetAsync(string categoryId, decimal limit, string period = "Monthly", int? month = null, int? year = null, decimal alertThreshold = 0.8m, CancellationToken ct = default);
    Task UpdateBudgetLimitAsync(string id, decimal limit, CancellationToken ct = default);

    // Metas
    Task<List<Data.Entities.LocalGoal>> GetGoalsAsync(CancellationToken ct = default);
    Task<Data.Entities.LocalGoal> CreateGoalAsync(string name, decimal targetAmount, DateTime? deadline = null, CancellationToken ct = default);
    Task ContributeToGoalAsync(string goalId, decimal amount, string? sourceAccountId = null, string? note = null, CancellationToken ct = default);

    // Transações Recorrentes
    Task<List<Data.Entities.LocalRecurringTransaction>> GetRecurringTransactionsAsync(CancellationToken ct = default);
    Task<Data.Entities.LocalRecurringTransaction> CreateRecurringTransactionAsync(string accountId, string type, decimal amount, string description, string frequency, DateTime startDate, string? categoryId = null, CancellationToken ct = default);
    Task<int> ExecuteDueRecurringTransactionsAsync(CancellationToken ct = default);
    Task DeactivateRecurringTransactionAsync(string id, CancellationToken ct = default);

    // Relatórios e Exportação
    Task<LocalMonthlyReport> GetMonthlyReportAsync(int month, int year, CancellationToken ct = default);
    Task<byte[]> ExportReportPdfAsync(int month, int year, CancellationToken ct = default);
    Task<string> ExportTransactionsCsvAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);
}

public sealed record LocalBudgetWithProgress(
    string Id,
    string CategoryId,
    string CategoryName,
    decimal Limit,
    decimal Spent,
    decimal Remaining,
    decimal Progress,
    decimal AlertThreshold,
    bool IsAlertTriggered,
    bool IsExceeded);

public sealed record LocalMonthlyReport(
    int Month,
    int Year,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetSavings,
    decimal SavingsRate,
    IReadOnlyList<CategoryExpenseSummary> CategoryExpenses,
    IReadOnlyList<DailyExpenseSummary> DailyExpenses);

public sealed record CategoryExpenseSummary(string CategoryName, decimal TotalAmount, decimal Percentage);
public sealed record DailyExpenseSummary(int Day, decimal Amount);
