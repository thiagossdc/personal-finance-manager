namespace PersonalFinance.Maui.Core.Services;

/// <summary>
/// Serviço de localização. Abstrai recursos de string para suporte a i18n.
/// </summary>
public interface ILocalizationService
{
    string this[string key] { get; }
    string GetString(string key);
}
