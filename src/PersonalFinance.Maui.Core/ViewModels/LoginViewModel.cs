using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PersonalFinance.Maui.Core.Data;
using PersonalFinance.Maui.Core.Services;
using PersonalFinance.Maui.Core.Sync;

namespace PersonalFinance.Maui.Core.ViewModels;

public sealed partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthApiClient _authClient;
    private readonly ITokenStorage _tokenStorage;
    private readonly SyncEngine _syncEngine;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    public LoginViewModel(
        LocalDbContext localDb,
        IAuthApiClient authClient,
        ITokenStorage tokenStorage,
        SyncEngine syncEngine,
        INavigationService navigationService) : base(localDb)
    {
        _authClient = authClient;
        _tokenStorage = tokenStorage;
        _syncEngine = syncEngine;
        _navigationService = navigationService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _authClient.LoginAsync(Email, Password);
            if (result is null)
            {
                throw new InvalidOperationException("E-mail ou senha inválidos.");
            }

            await _tokenStorage.SaveTokensAsync(result.AccessToken, result.RefreshToken);
            if (_authClient is AuthApiClient client)
            {
                client.SetAccessToken(result.AccessToken);
            }

            await _navigationService.NavigateToAsync("//dashboard");
        }, "Falha no login. Verifique suas credenciais.");
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        await ExecuteAsync(async () =>
        {
            var displayName = Email.Split('@')[0];
            var result = await _authClient.RegisterAsync(Email, displayName, Password);
            if (result is null)
            {
                throw new InvalidOperationException("Não foi possível criar a conta.");
            }

            await _tokenStorage.SaveTokensAsync(result.AccessToken, result.RefreshToken);
            await _navigationService.NavigateToAsync("//dashboard");
        }, "Falha no registro.");
    }
}
