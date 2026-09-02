using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Maui.Core.Services;
using PersonalFinance.Maui.Core.Sync;

namespace PersonalFinance.Maui;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly ITokenStorage _tokenStorage;

    public App(ITokenStorage tokenStorage)
    {
        _tokenStorage = tokenStorage;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

        _ = Task.Run(async () =>
        {
            var token = await _tokenStorage.GetAccessTokenAsync();
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (string.IsNullOrEmpty(token))
                {
                    await Shell.Current.GoToAsync("//login");
                }
                else if (MauiProgram.Services.GetService<IAuthApiClient>() is AuthApiClient client)
                {
                    client.SetAccessToken(token);
                }
            });
        });

        return window;
    }
}
