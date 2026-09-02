using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PersonalFinance.Domain.Common;

namespace PersonalFinance.Infrastructure.Persistence;

/// <summary>
/// Incrementa a versão otimista das entidades <see cref="IVersioned"/> antes de cada save,
/// centralizando a detecção de conflitos da sincronização.
/// </summary>
public sealed class VersionedEntityInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var entities = eventData.Context?.ChangeTracker
            .Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .Select(e => e.Entity)
            .OfType<IVersioned>();

        if (entities is not null)
        {
            foreach (var entity in entities)
            {
                entity.BumpVersion();
            }
        }

        return ValueTask.FromResult(result);
    }
}
