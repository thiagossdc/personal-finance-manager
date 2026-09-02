using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PersonalFinance.Maui.Core.Data;
using PersonalFinance.Maui.Core.Data.Entities;
using PersonalFinance.Maui.Core.Services;

namespace PersonalFinance.Maui.Core.ViewModels;

public sealed partial class CreditCardsViewModel : BaseViewModel
{
    private readonly ILocalFinanceService _financeService;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private decimal _creditLimit;

    [ObservableProperty]
    private int _closingDay = 10;

    [ObservableProperty]
    private int _dueDay = 17;

    [ObservableProperty]
    private string _lastFourDigits = string.Empty;

    [ObservableProperty]
    private LocalCreditCard? _selectedCard;

    // Formulário de compra
    [ObservableProperty]
    private string _purchaseDescription = string.Empty;

    [ObservableProperty]
    private decimal _purchaseAmount;

    [ObservableProperty]
    private int _installmentCount = 1;

    // Formulário de pagamento
    [ObservableProperty]
    private decimal _paymentAmount;

    [ObservableProperty]
    private LocalAccount? _paymentAccount;

    public ObservableCollection<LocalCreditCard> Cards { get; } = new();
    public ObservableCollection<LocalAccount> Accounts { get; } = new();
    public IReadOnlyList<int> InstallmentOptions { get; } = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 18, 24];

    public CreditCardsViewModel(LocalDbContext localDb, ILocalFinanceService financeService) : base(localDb)
    {
        _financeService = financeService;
        LoadCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await ExecuteAsync(async () =>
        {
            var cards = await _financeService.GetCreditCardsAsync();
            Cards.Clear();
            foreach (var card in cards)
            {
                Cards.Add(card);
            }

            var accounts = await _financeService.GetAccountsAsync();
            Accounts.Clear();
            foreach (var acc in accounts)
            {
                Accounts.Add(acc);
            }

            SelectedCard ??= Cards.FirstOrDefault();
            PaymentAccount ??= Accounts.FirstOrDefault();
        });
    }

    [RelayCommand]
    private async Task CreateCardAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            SetError("Informe o nome do cartão.");
            return;
        }

        if (CreditLimit <= 0)
        {
            SetError("O limite deve ser maior que zero.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            await _financeService.CreateCreditCardAsync(Name, CreditLimit, ClosingDay, DueDay, string.IsNullOrWhiteSpace(LastFourDigits) ? null : LastFourDigits);
            Name = string.Empty;
            CreditLimit = 0;
            LastFourDigits = string.Empty;
            await LoadAsync();
        }, "Não foi possível cadastrar o cartão.");
    }

    [RelayCommand]
    private async Task AddPurchaseAsync()
    {
        if (SelectedCard is null)
        {
            SetError("Selecione um cartão.");
            return;
        }

        if (string.IsNullOrWhiteSpace(PurchaseDescription) || PurchaseAmount <= 0)
        {
            SetError("Informe a descrição e o valor da compra.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            await _financeService.RecordCreditCardPurchaseAsync(
                SelectedCard.Id, PurchaseDescription, PurchaseAmount, InstallmentCount);
            PurchaseDescription = string.Empty;
            PurchaseAmount = 0;
            InstallmentCount = 1;
            await LoadAsync();
        }, "Não foi possível registrar a compra.");
    }

    [RelayCommand]
    private async Task PayCardAsync()
    {
        if (SelectedCard is null || PaymentAccount is null)
        {
            SetError("Selecione o cartão e a conta de pagamento.");
            return;
        }

        if (PaymentAmount <= 0)
        {
            SetError("Informe o valor do pagamento.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            await _financeService.PayCreditCardAsync(SelectedCard.Id, PaymentAccount.Id, PaymentAmount);
            PaymentAmount = 0;
            await LoadAsync();
        }, "Não foi possível realizar o pagamento.");
    }
}
