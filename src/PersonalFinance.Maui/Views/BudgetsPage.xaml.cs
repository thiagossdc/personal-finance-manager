using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Maui.Core.ViewModels;

namespace PersonalFinance.Maui.Views;

public partial class BudgetsPage : ContentPage
{
    public BudgetsPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<BudgetsViewModel>();
    }
}
