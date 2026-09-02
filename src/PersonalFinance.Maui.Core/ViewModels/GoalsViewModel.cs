using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PersonalFinance.Maui.Core.Data;
using PersonalFinance.Maui.Core.Data.Entities;
using PersonalFinance.Maui.Core.Services;

namespace PersonalFinance.Maui.Core.ViewModels;

public sealed partial class GoalsViewModel : BaseViewModel
{
    private readonly ILocalFinanceService _financeService;

    // Formulário de criação
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private decimal _targetAmount;

    [ObservableProperty]
    private DateTime _deadline = DateTime.UtcNow.AddMonths(12);

    // Formulário de aporte
    [ObservableProperty]
    private LocalGoal? _selectedGoal;

    [ObservableProperty]
    private decimal _contributionAmount;

    [ObservableProperty]
    private LocalAccount? _contributionAccount;

    public ObservableCollection<LocalGoal> Goals { get; } = new();
    public ObservableCollection<LocalAccount> Accounts { get; } = new();

    public GoalsViewModel(LocalDbContext localDb, ILocalFinanceService financeService) : base(localDb)
    {
        _financeService = financeService;
        LoadCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await ExecuteAsync(async () =>
        {
            var goals = await _financeService.GetGoalsAsync();
            Goals.Clear();
            foreach (var g in goals)
            {
                Goals.Add(g);
            }

            var accounts = await _financeService.GetAccountsAsync();
            Accounts.Clear();
            foreach (var a in accounts)
            {
                Accounts.Add(a);
            }

            SelectedGoal ??= Goals.FirstOrDefault();
            ContributionAccount ??= Accounts.FirstOrDefault();
        });
    }

    [RelayCommand]
    private async Task CreateGoalAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            SetError("Informe o nome da meta.");
            return;
        }

        if (TargetAmount <= 0)
        {
            SetError("O valor alvo deve ser maior que zero.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            await _financeService.CreateGoalAsync(Name, TargetAmount, Deadline);
            Name = string.Empty;
            TargetAmount = 0;
            await LoadAsync();
        }, "Não foi possível criar a meta.");
    }

    [RelayCommand]
    private async Task ContributeAsync()
    {
        if (SelectedGoal is null)
        {
            SetError("Selecione uma meta.");
            return;
        }

        if (ContributionAmount <= 0)
        {
            SetError("Informe o valor da contribuição.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            await _financeService.ContributeToGoalAsync(
                SelectedGoal.Id, ContributionAmount, ContributionAccount?.Id, "Aporte para meta");
            ContributionAmount = 0;
            await LoadAsync();
        }, "Não foi possível registrar o aporte.");
    }
}
