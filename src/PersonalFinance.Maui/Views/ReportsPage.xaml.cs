using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Maui.Core.ViewModels;

namespace PersonalFinance.Maui.Views;

public partial class ReportsPage : ContentPage
{
    public ReportsPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<ReportsViewModel>();
    }
}
