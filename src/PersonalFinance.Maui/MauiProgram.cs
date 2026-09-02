using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PersonalFinance.Maui.Core.Data;
using PersonalFinance.Maui.Core.Services;
using PersonalFinance.Maui.Core.Sync;
using PersonalFinance.Maui.Core.ViewModels;
using PersonalFinance.Maui.Services;
using PersonalFinance.Maui.Views;

namespace PersonalFinance.Maui;

public static class MauiProgram
{
    public static IServiceProvider Services { get; private set; } = default!;

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { });

        var services = builder.Services;

        // Banco local (offline-first)
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "personalfinance_local.db3");
        services.AddSingleton(_ => new LocalDbContext(dbPath));
        services.AddSingleton<ISyncStateStore>(_ => new SqliteSyncStateStore(dbPath));

        // Serviços de plataforma
        services.AddSingleton<ITokenStorage, SecureTokenStorage>();
        services.AddSingleton<IConnectivityService, DeviceConnectivityService>();
        services.AddSingleton<INavigationService, ShellNavigationService>();
        services.AddSingleton<ILocalizationService, ResxLocalizationService>();

        // HttpClient + clientes da API
        services.AddHttpClient<AuthApiClient>(client =>
        {
            client.BaseAddress = new Uri(AppSettings.GetApiBaseUrl());
            client.DefaultRequestHeaders.Accept.Add(new("application/json"));
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<IAuthApiClient>(sp => sp.GetRequiredService<AuthApiClient>());
        services.AddHttpClient<ISyncApiClient, SyncApiClient>(client =>
        {
            client.BaseAddress = new Uri(AppSettings.GetApiBaseUrl());
            client.DefaultRequestHeaders.Accept.Add(new("application/json"));
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddSingleton<IAuthApiClient, AuthApiClient>();
        services.AddSingleton<ILocalFinanceService, LocalFinanceService>();

        // Sincronização
        services.AddSingleton<SyncChangeMerger>();
        services.AddSingleton<SyncEngine>();

        // ViewModels
        services.AddSingleton<LoginViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<AccountsViewModel>();
        services.AddSingleton<TransactionsViewModel>();
        services.AddSingleton<CategoriesViewModel>();
        services.AddSingleton<BudgetsViewModel>();
        services.AddSingleton<GoalsViewModel>();
        services.AddSingleton<CreditCardsViewModel>();
        services.AddSingleton<RecurringTransactionsViewModel>();
        services.AddSingleton<ReportsViewModel>();
        services.AddSingleton<SettingsViewModel>();

        // Páginas
        services.AddSingleton<LoginPage>();
        services.AddSingleton<DashboardPage>();
        services.AddSingleton<AccountsPage>();
        services.AddSingleton<TransactionsPage>();
        services.AddSingleton<CategoriesPage>();
        services.AddSingleton<BudgetsPage>();
        services.AddSingleton<GoalsPage>();
        services.AddSingleton<CreditCardsPage>();
        services.AddSingleton<RecurringTransactionsPage>();
        services.AddSingleton<ReportsPage>();
        services.AddSingleton<SettingsPage>();

        var app = builder.Build();
        Services = app.Services;
        return app;
    }
}
