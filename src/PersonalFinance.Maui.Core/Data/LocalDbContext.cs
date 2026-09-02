using PersonalFinance.Maui.Core.Data.Entities;
using SQLite;

namespace PersonalFinance.Maui.Core.Data;

/// <summary>
/// Contexto local SQLite para funcionamento offline.
/// Todas as operações do app são feitas aqui e depois sincronizadas.
/// </summary>
public sealed class LocalDbContext
{
    public LocalDbContext(string dbPath)
    {
        Database = new SQLiteAsyncConnection(dbPath);
        InitializeAsync().ConfigureAwait(false);
    }

    private async Task InitializeAsync()
    {
        await Database.CreateTableAsync<LocalAccount>();
        await Database.CreateTableAsync<LocalCategory>();
        await Database.CreateTableAsync<LocalTransaction>();
        await Database.CreateTableAsync<LocalTransfer>();
        await Database.CreateTableAsync<LocalCreditCard>();
        await Database.CreateTableAsync<LocalBudget>();
        await Database.CreateTableAsync<LocalGoal>();
        await Database.CreateTableAsync<LocalRecurringTransaction>();
        await Database.CreateTableAsync<SyncQueueItem>();
    }

    public SQLiteAsyncConnection Database { get; }

    // ---- Contas ----
    public Task<List<LocalAccount>> GetAccountsAsync() =>
        Database.Table<LocalAccount>().Where(a => a.IsActive).ToListAsync();

    public async Task<LocalAccount?> GetAccountAsync(string id) =>
        await Database.Table<LocalAccount>().Where(a => a.Id == id).FirstOrDefaultAsync();

    public Task<int> SaveAccountAsync(LocalAccount account) =>
        account.LocalId == 0 ? Database.InsertAsync(account) : Database.UpdateAsync(account);

    public Task<int> DeleteAccountAsync(LocalAccount account) =>
        Database.DeleteAsync(account);

    // ---- Transações ----
    public Task<List<LocalTransaction>> GetTransactionsAsync(int limit = 100) =>
        Database.Table<LocalTransaction>()
            .OrderByDescending(t => t.TransactionDate)
            .Take(limit)
            .ToListAsync();

    public Task<List<LocalTransaction>> GetTransactionsByAccountAsync(string accountId) =>
        Database.Table<LocalTransaction>()
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();

    public Task<int> SaveTransactionAsync(LocalTransaction transaction) =>
        transaction.LocalId == 0 ? Database.InsertAsync(transaction) : Database.UpdateAsync(transaction);

    public async Task<LocalTransaction?> GetTransactionAsync(string id) =>
        await Database.Table<LocalTransaction>().Where(t => t.Id == id).FirstOrDefaultAsync();

    public Task<int> DeleteTransactionAsync(LocalTransaction transaction) =>
        Database.DeleteAsync(transaction);

    // ---- Categorias ----
    public Task<List<LocalCategory>> GetCategoriesAsync() =>
        Database.Table<LocalCategory>().Where(c => c.IsActive).ToListAsync();

    public async Task<LocalCategory?> GetCategoryAsync(string id) =>
        await Database.Table<LocalCategory>().Where(c => c.Id == id).FirstOrDefaultAsync();

    public Task<int> SaveCategoryAsync(LocalCategory category) =>
        category.LocalId == 0 ? Database.InsertAsync(category) : Database.UpdateAsync(category);

    public Task<int> DeleteCategoryAsync(LocalCategory category) =>
        Database.DeleteAsync(category);

    // ---- Transferências ----
    public Task<List<LocalTransfer>> GetTransfersAsync() =>
        Database.Table<LocalTransfer>().OrderByDescending(t => t.TransferDate).ToListAsync();

    public Task<int> SaveTransferAsync(LocalTransfer transfer) =>
        transfer.LocalId == 0 ? Database.InsertAsync(transfer) : Database.UpdateAsync(transfer);

    // ---- Cartões de Crédito ----
    public Task<List<LocalCreditCard>> GetCreditCardsAsync() =>
        Database.Table<LocalCreditCard>().Where(c => c.Status == "Active").ToListAsync();

    public async Task<LocalCreditCard?> GetCreditCardAsync(string id) =>
        await Database.Table<LocalCreditCard>().Where(c => c.Id == id).FirstOrDefaultAsync();

    public Task<int> SaveCreditCardAsync(LocalCreditCard card) =>
        card.LocalId == 0 ? Database.InsertAsync(card) : Database.UpdateAsync(card);

    // ---- Orçamentos ----
    public Task<List<LocalBudget>> GetBudgetsAsync() =>
        Database.Table<LocalBudget>().ToListAsync();

    public async Task<LocalBudget?> GetBudgetAsync(string id) =>
        await Database.Table<LocalBudget>().Where(b => b.Id == id).FirstOrDefaultAsync();

    public Task<int> SaveBudgetAsync(LocalBudget budget) =>
        budget.LocalId == 0 ? Database.InsertAsync(budget) : Database.UpdateAsync(budget);

    // ---- Metas ----
    public Task<List<LocalGoal>> GetGoalsAsync() =>
        Database.Table<LocalGoal>().ToListAsync();

    public async Task<LocalGoal?> GetGoalAsync(string id) =>
        await Database.Table<LocalGoal>().Where(g => g.Id == id).FirstOrDefaultAsync();

    public Task<int> SaveGoalAsync(LocalGoal goal) =>
        goal.LocalId == 0 ? Database.InsertAsync(goal) : Database.UpdateAsync(goal);

    // ---- Transações Recorrentes ----
    public Task<List<LocalRecurringTransaction>> GetRecurringTransactionsAsync() =>
        Database.Table<LocalRecurringTransaction>().Where(r => r.IsActive).ToListAsync();

    public async Task<LocalRecurringTransaction?> GetRecurringTransactionAsync(string id) =>
        await Database.Table<LocalRecurringTransaction>().Where(r => r.Id == id).FirstOrDefaultAsync();

    public Task<int> SaveRecurringTransactionAsync(LocalRecurringTransaction recurring) =>
        recurring.LocalId == 0 ? Database.InsertAsync(recurring) : Database.UpdateAsync(recurring);

    // ---- Fila de Sincronização ----
    public Task<List<SyncQueueItem>> GetPendingSyncItemsAsync() =>
        Database.Table<SyncQueueItem>()
            .Where(s => s.SyncStatus == SyncStatusNames.Pending || s.SyncStatus == SyncStatusNames.Failed)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

    public Task<int> EnqueueSyncAsync(SyncQueueItem item) =>
        Database.InsertAsync(item);

    public Task<int> UpdateSyncItemAsync(SyncQueueItem item) =>
        Database.UpdateAsync(item);

    public Task<int> RemoveSyncItemAsync(SyncQueueItem item) =>
        Database.DeleteAsync(item);
}
