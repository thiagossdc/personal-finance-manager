namespace PersonalFinance.Domain.Common;

/// <summary>
/// Entidade versionada, usada para detecção de conflitos na sincronização
/// (optimistic concurrency entre dispositivo local e servidor).
/// </summary>
public interface IVersioned
{
    long Version { get; }

    void BumpVersion();
}
