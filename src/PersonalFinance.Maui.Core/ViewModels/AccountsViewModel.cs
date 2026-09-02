using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PersonalFinance.Maui.Core.Data;
using PersonalFinance.Maui.Core.Data.Entities;
using PersonalFinance.Maui.Core.Services;

namespace PersonalFinance.Maui.Core.ViewModels;

public sealed partial class AccountsViewModel : BaseViewModel
{
    private readonly ILocalFinanceService _financeService;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _selectedType = "Checking";

    [ObservableProperty]
    private decimal _initialBalance;

    public ObservableCollection<LocalAccount> Accounts { get; } = new();
    public IReadOnlyList<string> AccountTypes { get; } = ["Checking", "Savings", "Wallet", "Investment", "Other"];

    public AccountsViewModel(LocalDbContext localDb, ILocalFinanceService financeService) : base(localDb)
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
        });
    }

    [RelayCommand]
    private async Task CreateAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            SetError("Informe o nome da conta.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            await _financeService.CreateAccountAsync(Name, SelectedType, InitialBalance);
            Name = string.Empty;
            InitialBalance = 0;
            await LoadAsync();
        }, "Não foi possível criar a conta.");
    }

    [RelayCommand]
    private async Task DeleteAccountAsync(LocalAccount account)
    {
        if (account is null) return;
        await ExecuteAsync(async () =>
        {
            await _financeService.DeleteAccountAsync(account.Id);
            await LoadAsync();
        }, "Não foi possível remover a conta.");
    }
}
