using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PersonalFinance.Maui.Core.Data;
using PersonalFinance.Maui.Core.Data.Entities;
using PersonalFinance.Maui.Core.Services;
using PersonalFinance.Maui.Core.Sync;

namespace PersonalFinance.Maui.Core.ViewModels;

public sealed partial class DashboardViewModel : BaseViewModel
{
    private readonly SyncEngine _syncEngine;
    private readonly IConnectivityService _connectivity;
    [ObservableProperty]
    private decimal _currentBalance;

    [ObservableProperty]
    private decimal _monthlyIncome;

    [ObservableProperty]
    private decimal _monthlyExpense;

    [ObservableProperty]
    private decimal _monthlySavings;

    [ObservableProperty]
    private string _selectedMonth = DateTime.Now.ToString("MM/yyyy", CultureInfo.InvariantCulture);

    public ObservableCollection<LocalAccount> Accounts { get; } = new();
    public ObservableCollection<LocalTransaction> RecentTransactions { get; } = new();

    public DashboardViewModel(LocalDbContext localDb, SyncEngine syncEngine, IConnectivityService connectivity) : base(localDb)
    {
        _syncEngine = syncEngine;
        _connectivity = connectivity;
        LoadDataCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            var accounts = await LocalDb.GetAccountsAsync();
            var transactions = await LocalDb.GetTransactionsAsync(10);

            Accounts.Clear();
            foreach (var account in accounts)
                Accounts.Add(account);

            RecentTransactions.Clear();
            foreach (var transaction in transactions)
                RecentTransactions.Add(transaction);

            CurrentBalance = accounts.Sum(a => a.CurrentBalance);

            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            MonthlyIncome = transactions
                .Where(t => t.Type == "Income" && t.TransactionDate.Month == currentMonth && t.TransactionDate.Year == currentYear)
                .Sum(t => t.Amount);
            MonthlyExpense = transactions
                .Where(t => t.Type == "Expense" && t.TransactionDate.Month == currentMonth && t.TransactionDate.Year == currentYear)
                .Sum(t => t.Amount);
            MonthlySavings = MonthlyIncome - MonthlyExpense;
        });
    }

    [RelayCommand]
    private async Task SyncAsync()
    {
        if (!_connectivity.IsConnected)
        {
            SetError("Sem conexão com a internet.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _syncEngine.SyncAsync();
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new InvalidOperationException(result.Error);
            }

            await LoadDataAsync();
        }, "Falha ao sincronizar. Tente novamente.");
    }
}
