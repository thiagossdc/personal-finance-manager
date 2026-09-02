using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Application.Dtos;

// ---- Dashboard ----
public sealed record DashboardDto(
    decimal CurrentBalance,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal TotalSavings,
    int Month,
    int Year,
    IReadOnlyList<TransactionDto> RecentTransactions,
    IReadOnlyList<BudgetDto> BudgetStatus,
    IReadOnlyList<GoalDto> Goals,
    IReadOnlyList<SeriesPoint> IncomeSeries,
    IReadOnlyList<SeriesPoint> ExpenseSeries,
    IReadOnlyList<CategorySlice> ExpenseByCategory,
    IReadOnlyList<SeriesPoint> NetWorthSeries);

public sealed record SeriesPoint(string Label, decimal Value);
public sealed record CategorySlice(string Label, decimal Value);

// ---- Sync ----
public sealed record SyncPushItem(
    string EntityName,
    string EntityId,
    string ClientOperationId,
    SyncChangeType ChangeType,
    string? Payload,
    long BaseVersion);

public sealed record SyncPushRequest(IReadOnlyList<SyncPushItem> Items);

public sealed record SyncServerChange(
    string EntityName,
    string EntityId,
    SyncChangeType ChangeType,
    long Version,
    string? Payload);

public sealed record SyncPullRequest(string? LastSyncToken, int Page = 1, int PageSize = 200);

public sealed record SyncPullResponse(
    IReadOnlyList<SyncServerChange> Changes,
    string? NextSyncToken,
    int Total,
    bool HasMore);

public sealed record SyncPushResult(int Applied, IReadOnlyList<string> Conflicts);

/// <summary>Resultado de conflito detectado durante upload.</summary>
public sealed record ConflictResult(string EntityName, string EntityId, string ClientOperationId, long ServerVersion);
