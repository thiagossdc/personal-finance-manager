using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Maui.Core.ViewModels;

namespace PersonalFinance.Maui.Views;

public partial class RecurringTransactionsPage : ContentPage
{
    public RecurringTransactionsPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<RecurringTransactionsViewModel>();
    }
}
