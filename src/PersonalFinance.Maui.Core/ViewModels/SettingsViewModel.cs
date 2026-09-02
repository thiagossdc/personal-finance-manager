using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PersonalFinance.Maui.Core.Data;
using PersonalFinance.Maui.Core.Services;
using PersonalFinance.Maui.Core.Sync;

namespace PersonalFinance.Maui.Core.ViewModels;

public sealed partial class SettingsViewModel : BaseViewModel
{
    private readonly ITokenStorage _tokenStorage;
    private readonly INavigationService _navigationService;
    private readonly SyncEngine _syncEngine;
    private readonly IConnectivityService _connectivity;

    [ObservableProperty]
    private string _selectedTheme = "System";

    [ObservableProperty]
    private string _lastSyncStatus = "Pronto";

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    [ObservableProperty]
    private string _language = "Português (Brasil)";

    public IReadOnlyList<string> ThemeOptions { get; } = ["System", "Light", "Dark"];

    public SettingsViewModel(
        LocalDbContext localDb,
        ITokenStorage tokenStorage,
        INavigationService navigationService,
        SyncEngine syncEngine,
        IConnectivityService connectivity) : base(localDb)
    {
        _tokenStorage = tokenStorage;
        _navigationService = navigationService;
        _syncEngine = syncEngine;
        _connectivity = connectivity;
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        if (!_connectivity.IsConnected)
        {
            SetError("Sem conexão com a internet.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            LastSyncStatus = "Sincronizando...";
            var result = await _syncEngine.SyncAsync();
            if (!string.IsNullOrEmpty(result.Error))
            {
                LastSyncStatus = $"Erro: {result.Error}";
                throw new InvalidOperationException(result.Error);
            }

            LastSyncStatus = $"Sincronizado: {result.Pushed} enviados, {result.Pulled} recebidos";
        }, "Falha ao sincronizar dados com o servidor.");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await ExecuteAsync(async () =>
        {
            await _tokenStorage.ClearAsync();
            await _navigationService.NavigateToAsync("//login");
        }, "Erro ao encerrar sessão.");
    }
}
