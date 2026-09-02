using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PersonalFinance.Maui.Core.Data;
using PersonalFinance.Maui.Core.Services;

namespace PersonalFinance.Maui.Core.ViewModels;

public sealed partial class ReportsViewModel : BaseViewModel
{
    private readonly ILocalFinanceService _financeService;

    [ObservableProperty]
    private int _selectedMonth = DateTime.UtcNow.Month;

    [ObservableProperty]
    private int _selectedYear = DateTime.UtcNow.Year;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoCategories))]
    private decimal _totalIncome;

    [ObservableProperty]
    private decimal _totalExpense;

    [ObservableProperty]
    private decimal _netSavings;

    [ObservableProperty]
    private decimal _savingsRate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCsvResult))]
    private string? _csvExportResult;

    [ObservableProperty]
    private byte[]? _lastPdfBytes;

    public ObservableCollection<CategoryExpenseSummary> CategorySummaries { get; } = new();
    public ObservableCollection<DailyExpenseSummary> DailySummaries { get; } = new();

    public bool HasNoCategories => CategorySummaries.Count == 0;
    public bool HasCsvResult => !string.IsNullOrEmpty(CsvExportResult);

    public IReadOnlyList<int> Months { get; } = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
    public IReadOnlyList<int> Years { get; } = [2024, 2025, 2026, 2027, 2028];

    public ReportsViewModel(LocalDbContext localDb, ILocalFinanceService financeService) : base(localDb)
    {
        _financeService = financeService;
        GenerateReportCommand.Execute(null);
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        await ExecuteAsync(async () =>
        {
            var report = await _financeService.GetMonthlyReportAsync(SelectedMonth, SelectedYear);
            TotalIncome = report.TotalIncome;
            TotalExpense = report.TotalExpense;
            NetSavings = report.NetSavings;
            SavingsRate = report.SavingsRate;

            CategorySummaries.Clear();
            foreach (var item in report.CategoryExpenses)
            {
                CategorySummaries.Add(item);
            }

            DailySummaries.Clear();
            foreach (var item in report.DailyExpenses)
            {
                DailySummaries.Add(item);
            }

            OnPropertyChanged(nameof(HasNoCategories));
        });
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        await ExecuteAsync(async () =>
        {
            var from = new DateTime(SelectedYear, SelectedMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddMonths(1).AddDays(-1);
            CsvExportResult = await _financeService.ExportTransactionsCsvAsync(from, to);
        }, "Não foi possível gerar a exportação CSV.");
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        await ExecuteAsync(async () =>
        {
            LastPdfBytes = await _financeService.ExportReportPdfAsync(SelectedMonth, SelectedYear);
        }, "Não foi possível gerar a exportação em PDF.");
    }
}
