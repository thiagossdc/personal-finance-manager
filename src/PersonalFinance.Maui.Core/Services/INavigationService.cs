namespace PersonalFinance.Maui.Core.Services;

/// <summary>
/// Abstração de navegação. Permite testar ViewModels sem depender do Shell do MAUI.
/// </summary>
public interface INavigationService
{
    Task NavigateToAsync(string route);
    Task NavigateToAsync(string route, IDictionary<string, object> parameters);
    Task GoBackAsync();
}
