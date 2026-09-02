namespace PersonalFinance.Maui;

/// <summary>
/// Configurações do aplicativo. Em produção, mova para configuração por ambiente.
/// </summary>
public static class AppSettings
{
    /// <summary>
    /// URL base da API.
    /// - Simulador/dispositivo macOS/iOS: http://localhost:5224
    /// - Emulador Android: http://10.0.2.2:5224 (loopback do host)
    /// - Dispositivo físico: use o IP da máquina na rede local.
    /// </summary>
    public static string GetApiBaseUrl() =>
#if ANDROID
        "http://10.0.2.2:5224/";
#else
        "http://localhost:5224/";
#endif
}
