using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PersonalFinance.Maui.Core.Data;
using PersonalFinance.Maui.Core.Data.Entities;
using PersonalFinance.Maui.Core.Services;

namespace PersonalFinance.Maui.Core.ViewModels;

public sealed partial class RecurringTransactionsViewModel : BaseViewModel
{
    private readonly ILocalFinanceService _financeService;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private string _selectedType = "Expense";

    [ObservableProperty]
    private string _selectedFrequency = "Monthly";

    [ObservableProperty]
    private DateTime _startDate = DateTime.UtcNow;

    [ObservableProperty]
    private LocalAccount? _selectedAccount;

    [ObservableProperty]
    private LocalCategory? _selectedCategory;

    public ObservableCollection<LocalRecurringTransaction> RecurringItems { get; } = new();
    public ObservableCollection<LocalAccount> Accounts { get; } = new();
    public ObservableCollection<LocalCategory> Categories { get; } = new();
    public IReadOnlyList<string> Frequencies { get; } = ["Weekly", "Biweekly", "Monthly", "Yearly"];
    public IReadOnlyList<string> TransactionTypes { get; } = ["Expense", "Income"];

    public RecurringTransactionsViewModel(LocalDbContext localDb, ILocalFinanceService financeService) : base(localDb)
    {
        _financeService = financeService;
        LoadCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await ExecuteAsync(async () =>
        {
            var items = await _financeService.GetRecurringTransactionsAsync();
            RecurringItems.Clear();
            foreach (var item in items)
            {
                RecurringItems.Add(item);
            }

            var accounts = await _financeService.GetAccountsAsync();
            Accounts.Clear();
            foreach (var a in accounts)
            {
                Accounts.Add(a);
            }

            var categories = await _financeService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var c in categories)
            {
                Categories.Add(c);
            }

            SelectedAccount ??= Accounts.FirstOrDefault();
            SelectedCategory ??= Categories.FirstOrDefault();
        });
    }

    [RelayCommand]
    private async Task CreateRecurringAsync()
    {
        if (SelectedAccount is null)
        {
            SetError("Selecione uma conta.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Description) || Amount <= 0)
        {
            SetError("Informe a descrição e o valor.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            await _financeService.CreateRecurringTransactionAsync(
                SelectedAccount.Id, SelectedType, Amount, Description, SelectedFrequency, StartDate, SelectedCategory?.Id);
            Description = string.Empty;
            Amount = 0;
            await LoadAsync();
        }, "Não foi possível criar a transação recorrente.");
    }

    [RelayCommand]
    private async Task ExecuteDueAsync()
    {
        await ExecuteAsync(async () =>
        {
            var count = await _financeService.ExecuteDueRecurringTransactionsAsync();
            await LoadAsync();
        }, "Não foi possível processar as recorrências.");
    }

    [RelayCommand]
    private async Task DeactivateAsync(LocalRecurringTransaction item)
    {
        if (item is null) return;
        await ExecuteAsync(async () =>
        {
            await _financeService.DeactivateRecurringTransactionAsync(item.Id);
            await LoadAsync();
        }, "Não foi possível desativar a recorrência.");
    }
}
