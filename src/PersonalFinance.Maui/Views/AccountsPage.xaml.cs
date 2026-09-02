using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Maui.Core.ViewModels;

namespace PersonalFinance.Maui.Views;

public partial class AccountsPage : ContentPage
{
    public AccountsPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<AccountsViewModel>();
    }
}
