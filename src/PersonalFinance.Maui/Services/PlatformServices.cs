using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Maui.Core.Services;

namespace PersonalFinance.Maui.Services;

/// <summary>Nome do HttpClient nomeado usado pelos clientes da API.</summary>
public static class HttpClientNames
{
    public const string Api = "PersonalFinanceApi";
}

/// <summary>Armazenamento seguro de tokens usando SecureStorage da plataforma.</summary>
public sealed class SecureTokenStorage : ITokenStorage
{
    private const string AccessTokenKey = "pf_access_token";
    private const string RefreshTokenKey = "pf_refresh_token";

    public async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        await SecureStorage.SetAsync(AccessTokenKey, accessToken);
        await SecureStorage.SetAsync(RefreshTokenKey, refreshToken);
    }

    public Task<string?> GetAccessTokenAsync() => SecureStorage.GetAsync(AccessTokenKey);

    public Task<string?> GetRefreshTokenAsync() => SecureStorage.GetAsync(RefreshTokenKey);

    public Task ClearAsync()
    {
        SecureStorage.Remove(AccessTokenKey);
        SecureStorage.Remove(RefreshTokenKey);
        return Task.CompletedTask;
    }
}

/// <summary>Serviço de conectividade baseado em Microsoft.Maui.Essentials Connectivity.</summary>
public sealed class DeviceConnectivityService : IConnectivityService
{
    public DeviceConnectivityService()
    {
        Connectivity.ConnectivityChanged += (_, args) =>
        {
            ConnectivityChanged?.Invoke(this, args.NetworkAccess == NetworkAccess.Internet);
        };
    }

    public bool IsConnected => Connectivity.NetworkAccess == NetworkAccess.Internet;

    public event EventHandler<bool>? ConnectivityChanged;
}

/// <summary>Navegação via Shell, aderente às rotas usadas pelos ViewModels.</summary>
public sealed class ShellNavigationService : INavigationService
{
    public Task NavigateToAsync(string route) => Shell.Current.GoToAsync(route);

    public Task NavigateToAsync(string route, IDictionary<string, object> parameters) =>
        Shell.Current.GoToAsync(route, parameters);

    public async Task GoBackAsync()
    {
        if (Shell.Current.Navigation.NavigationStack.Count > 1)
        {
            await Shell.Current.Navigation.PopAsync();
        }
        else
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}

/// <summary>Localização baseada nos recursos .resx do PersonalFinance.Maui.Core.</summary>
public sealed class ResxLocalizationService : ILocalizationService
{
    private static readonly System.Resources.ResourceManager Manager =
        new("PersonalFinance.Maui.Core.Resources.Strings",
            typeof(LocalizationMarker).Assembly);

    public string this[string key] => GetString(key);

    public string GetString(string key)
    {
        var value = Manager.GetString(key, System.Globalization.CultureInfo.CurrentUICulture);
        return value ?? key;
    }
}

internal sealed class LocalizationMarker;
