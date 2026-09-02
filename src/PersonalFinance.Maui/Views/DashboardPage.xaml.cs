using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Maui.Core.ViewModels;

namespace PersonalFinance.Maui.Views;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<DashboardViewModel>();
    }
}
