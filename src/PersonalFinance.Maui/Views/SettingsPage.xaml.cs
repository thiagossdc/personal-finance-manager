using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Maui.Core.ViewModels;

namespace PersonalFinance.Maui.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<SettingsViewModel>();
    }
}
