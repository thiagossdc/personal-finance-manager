using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Maui.Core.ViewModels;

namespace PersonalFinance.Maui.Views;

public partial class TransactionsPage : ContentPage
{
    public TransactionsPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<TransactionsViewModel>();
    }
}
