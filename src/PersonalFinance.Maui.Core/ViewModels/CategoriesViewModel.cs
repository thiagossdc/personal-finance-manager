using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PersonalFinance.Maui.Core.Data;
using PersonalFinance.Maui.Core.Data.Entities;
using PersonalFinance.Maui.Core.Services;

namespace PersonalFinance.Maui.Core.ViewModels;

public sealed partial class CategoriesViewModel : BaseViewModel
{
    private readonly ILocalFinanceService _financeService;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _selectedType = "Expense";

    [ObservableProperty]
    private string _selectedIcon = "tag";

    [ObservableProperty]
    private LocalCategory? _selectedParentCategory;

    public ObservableCollection<LocalCategory> Categories { get; } = new();
    public ObservableCollection<LocalCategory> ParentCategories { get; } = new();
    public IReadOnlyList<string> CategoryTypes { get; } = ["Expense", "Income"];
    public IReadOnlyList<string> AvailableIcons { get; } = ["cart", "food", "car", "home", "heart", "school", "game", "briefcase", "cash", "bank", "tag"];

    public CategoriesViewModel(LocalDbContext localDb, ILocalFinanceService financeService) : base(localDb)
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
            ParentCategories.Clear();

            foreach (var cat in categories)
            {
                Categories.Add(cat);
                if (string.IsNullOrEmpty(cat.ParentId))
                {
                    ParentCategories.Add(cat);
                }
            }
        });
    }

    [RelayCommand]
    private async Task CreateCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            SetError("Informe o nome da categoria.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            await _financeService.CreateCategoryAsync(
                Name, SelectedType, SelectedIcon, SelectedParentCategory?.Id);
            Name = string.Empty;
            SelectedParentCategory = null;
            await LoadAsync();
        }, "Não foi possível criar a categoria.");
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync(LocalCategory category)
    {
        if (category is null) return;
        await ExecuteAsync(async () =>
        {
            await _financeService.DeleteCategoryAsync(category.Id);
            await LoadAsync();
        }, "Não foi possível desativar a categoria.");
    }
}
