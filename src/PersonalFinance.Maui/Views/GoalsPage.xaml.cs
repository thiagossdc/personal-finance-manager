using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Maui.Core.ViewModels;

namespace PersonalFinance.Maui.Views;

public partial class GoalsPage : ContentPage
{
    public GoalsPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<GoalsViewModel>();
    }
}
