using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PersonalFinance.Maui.Core.Data;
using PersonalFinance.Maui.Core.Data.Entities;
using PersonalFinance.Maui.Core.Services;

namespace PersonalFinance.Maui.Core.ViewModels;

public sealed partial class TransactionsViewModel : BaseViewModel
{
    private readonly ILocalFinanceService _financeService;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private string _selectedType = "Expense";

    [ObservableProperty]
    private LocalAccount? _selectedAccount;

    public ObservableCollection<LocalTransaction> Transactions { get; } = new();
    public ObservableCollection<LocalAccount> Accounts { get; } = new();
    public IReadOnlyList<string> TransactionTypes { get; } = ["Income", "Expense"];

    public TransactionsViewModel(LocalDbContext localDb, ILocalFinanceService financeService) : base(localDb)
    {
        _financeService = financeService;
        LoadCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await ExecuteAsync(async () =>
        {
            var accounts = await LocalDb.GetAccountsAsync();
            Accounts.Clear();
            foreach (var account in accounts)
            {
                Accounts.Add(account);
            }

            SelectedAccount ??= Accounts.FirstOrDefault();

            var transactions = await LocalDb.GetTransactionsAsync(50);
            Transactions.Clear();
            foreach (var tx in transactions)
            {
                Transactions.Add(tx);
            }
        });
    }

    [RelayCommand]
    private async Task AddTransactionAsync()
    {
        if (SelectedAccount is null)
        {
            SetError("Selecione uma conta.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            await _financeService.CreateTransactionAsync(
                SelectedAccount.Id, SelectedType, Amount, Description, DateTime.UtcNow);
            Description = string.Empty;
            Amount = 0;
            await LoadAsync();
        }, "Não foi possível registrar a transação.");
    }
}
