using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PersonalFinance.Maui.Core.Data;
using PersonalFinance.Maui.Core.Data.Entities;
using PersonalFinance.Maui.Core.Services;

namespace PersonalFinance.Maui.Core.ViewModels;

public sealed partial class BudgetsViewModel : BaseViewModel
{
    private readonly ILocalFinanceService _financeService;

    [ObservableProperty]
    private LocalCategory? _selectedCategory;

    [ObservableProperty]
    private decimal _limit;

    [ObservableProperty]
    private decimal _alertThreshold = 0.8m;

    [ObservableProperty]
    private int _selectedMonth = DateTime.UtcNow.Month;

    [ObservableProperty]
    private int _selectedYear = DateTime.UtcNow.Year;

    public ObservableCollection<LocalBudgetWithProgress> Budgets { get; } = new();
    public ObservableCollection<LocalCategory> Categories { get; } = new();

    public BudgetsViewModel(LocalDbContext localDb, ILocalFinanceService financeService) : base(localDb)
    {
        _financeService = financeService;
        LoadCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await ExecuteAsync(async () =>
        {
            var categories = await _financeService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var cat in categories.Where(c => c.Type == "Expense"))
            {
                Categories.Add(cat);
            }
            SelectedCategory ??= Categories.FirstOrDefault();

            var list = await _financeService.GetBudgetsAsync(SelectedMonth, SelectedYear);
            Budgets.Clear();
            foreach (var b in list)
            {
                Budgets.Add(b);
            }
        });
    }

    [RelayCommand]
    private async Task CreateBudgetAsync()
    {
        if (SelectedCategory is null)
        {
            SetError("Selecione uma categoria para o orçamento.");
            return;
        }

        if (Limit <= 0)
        {
            SetError("O limite do orçamento deve ser maior que zero.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            await _financeService.CreateBudgetAsync(
                SelectedCategory.Id, Limit, "Monthly", SelectedMonth, SelectedYear, AlertThreshold);
            Limit = 0;
            await LoadAsync();
        }, "Não foi possível criar o orçamento.");
    }
}
