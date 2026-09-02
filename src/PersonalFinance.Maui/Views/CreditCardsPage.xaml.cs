using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Maui.Core.ViewModels;

namespace PersonalFinance.Maui.Views;

public partial class CreditCardsPage : ContentPage
{
    public CreditCardsPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<CreditCardsViewModel>();
    }
}
