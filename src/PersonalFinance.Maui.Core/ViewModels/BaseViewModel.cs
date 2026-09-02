using CommunityToolkit.Mvvm.ComponentModel;
using PersonalFinance.Maui.Core.Data;

namespace PersonalFinance.Maui.Core.ViewModels;

/// <summary>
/// ViewModel base com suporte a loading e erros.
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy), nameof(IsNotBusy))]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    /// <summary>Alias para <see cref="IsLoading"/> — convenção padrão MAUI.</summary>
    public bool IsBusy => IsLoading;

    /// <summary>Inverso de <see cref="IsBusy"/> — usado para IsEnabled em botões durante carregamento.</summary>
    public bool IsNotBusy => !IsLoading;

    protected LocalDbContext LocalDb { get; }

    protected BaseViewModel(LocalDbContext localDb)
    {
        LocalDb = localDb;
    }

    protected void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    protected void ClearError()
    {
        ErrorMessage = null;
        HasError = false;
    }

    /// <summary>
    /// Executa ação assíncrona com loading e tratamento de erro.
    /// </summary>
    protected async Task<bool> ExecuteAsync(Func<Task> action, string? errorMessage = null)
    {
        IsLoading = true;
        ClearError();
        try
        {
            await action();
            return true;
        }
        catch (Exception ex)
        {
            SetError(errorMessage ?? ex.Message);
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
