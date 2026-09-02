using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Maui.Core.ViewModels;

namespace PersonalFinance.Maui.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<LoginViewModel>();
    }
}
