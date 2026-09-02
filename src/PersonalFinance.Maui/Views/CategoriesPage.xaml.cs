using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Maui.Core.ViewModels;

namespace PersonalFinance.Maui.Views;

public partial class CategoriesPage : ContentPage
{
    public CategoriesPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<CategoriesViewModel>();
    }
}
