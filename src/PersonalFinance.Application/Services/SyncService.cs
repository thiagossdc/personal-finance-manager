using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Application.Services;

/// <summary>
/// Serviço de sincronização (servidor). Recebe operações offline do dispositivo (push),
/// aplica de forma idempotente, e entrega alterações desde um token (pull).
/// Estratégia de conflito: last-write-wins simples com detecção por versão — quando o cliente
/// envia uma mudança baseada em versão mais antiga que a atual do servidor, o conflito é
/// devolvido ao cliente para resolução (documentado em docs/synchronization.md).
/// </summary>
public sealed partial class SyncService : ServiceBase, ISyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public SyncService(IAppDbContext db, ICurrentUser currentUser, IDateTime time)
        : base(db, currentUser, time)
    {
    }

    public async Task<Result<SyncPushResult>> PushAsync(SyncPushRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var applied = 0;
        var conflicts = new List<string>();

        foreach (var item in request.Items)
        {
            var alreadyApplied = await Db.SyncItems.AnyAsync(s =>
                s.UserId == userId && s.ClientOperationId == item.ClientOperationId && s.Status == SyncStatus.Synced, ct);
            if (alreadyApplied)
            {
                applied++;
                continue;
            }

            var result = await ApplyItemAsync(userId, item, ct);
            if (result.IsSuccess)
            {
                var recordResult = SyncItem.Create(userId, item.EntityName, item.EntityId, item.ChangeType, item.ClientOperationId);
                if (recordResult.IsSuccess)
                {
                    recordResult.Value!.MarkSynced(result.Value);
                    Db.SyncItems.Add(recordResult.Value!);
                }

                applied++;
            }
            else
            {
                if (item.ChangeType == SyncChangeType.Delete)
                {
                    // Idempotência para deletes já aplicados: trata como sucesso (registro ausente).
                    applied++;
                }
                else
                {
                    conflicts.Add(result.Error?.Message ?? $"Conflict on {item.EntityName}/{item.EntityId}");
                }
            }
        }

        // Uma única SaveChanges mantém todo o lote atômico. Se o índice único
        // (UserId, ClientOperationId) detectar uma operação já inserida por uma
        // requisição concorrente, a transação atual é descartada (nada persiste) e
        // contabilizamos apenas o que já estava no servidor — sem duplicar dados.
        try
        {
            await Db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var alreadyPersisted = 0;
            foreach (var item in request.Items)
            {
                var recorded = await Db.SyncItems.AnyAsync(s =>
                    s.UserId == userId && s.ClientOperationId == item.ClientOperationId && s.Status == SyncStatus.Synced, ct);
                if (recorded)
                {
                    alreadyPersisted++;
                }
            }

            return Result<SyncPushResult>.Success(new SyncPushResult(alreadyPersisted, conflicts));
        }

        return Result<SyncPushResult>.Success(new SyncPushResult(applied, conflicts));
    }

    private async Task<Result<long>> ApplyItemAsync(Guid userId, SyncPushItem item, CancellationToken ct)
    {
        return item.ChangeType switch
        {
            SyncChangeType.Delete => await ApplyDeleteAsync(userId, item, ct),
            _ => await ApplyUpsertAsync(userId, item, ct)
        };
    }

    private async Task<Result<long>> ApplyDeleteAsync(Guid userId, SyncPushItem item, CancellationToken ct)
    {
        var entityId = Guid.TryParse(item.EntityId, out var id) ? id : Guid.Empty;
        switch (item.EntityName)
        {
            case "Account":
                var account = await Db.Accounts.FirstOrDefaultAsync(a => a.Id == entityId && a.UserId == userId, ct);
                if (account is null)
                {
                    return Result<long>.Failure(Error.NotFound("Account not found for delete."));
                }

                account.Deactivate();
                return Result<long>.Success(account.Version);
            case "Transaction":
                var txn = await Db.Transactions.FirstOrDefaultAsync(t => t.Id == entityId && t.UserId == userId, ct);
                if (txn is null)
                {
                    return Result<long>.Failure(Error.NotFound("Transaction not found for delete."));
                }

                txn.MarkCanceled();
                return Result<long>.Success(txn.Version);
            case "Category":
                var category = await Db.Categories.FirstOrDefaultAsync(c => c.Id == entityId && c.UserId == userId, ct);
                if (category is null)
                {
                    return Result<long>.Failure(Error.NotFound("Category not found for delete."));
                }

                category.Deactivate();
                return Result<long>.Success(category.Version);
            case "Budget":
                var budget = await Db.Budgets.FirstOrDefaultAsync(b => b.Id == entityId && b.UserId == userId, ct);
                if (budget is not null)
                {
                    Db.Budgets.Remove(budget);
                }
                return Result<long>.Success(1);
            case "Goal":
                var goal = await Db.FinancialGoals.FirstOrDefaultAsync(g => g.Id == entityId && g.UserId == userId, ct);
                if (goal is not null)
                {
                    goal.Cancel();
                    return Result<long>.Success(goal.Version);
                }
                return Result<long>.Failure(Error.NotFound("Goal not found for delete."));
            case "RecurringTransaction":
                var recurring = await Db.RecurringTransactions.FirstOrDefaultAsync(r => r.Id == entityId && r.UserId == userId, ct);
                if (recurring is not null)
                {
                    recurring.Deactivate();
                    return Result<long>.Success(recurring.Version);
                }
                return Result<long>.Failure(Error.NotFound("Recurring transaction not found for delete."));
            default:
                return Result<long>.Failure(Error.Business($"Unsupported entity for sync: {item.EntityName}"));
        }
    }

    private async Task<Result<long>> ApplyUpsertAsync(Guid userId, SyncPushItem item, CancellationToken ct)
    {
        return item.EntityName switch
        {
            "Account" => await UpsertAccountAsync(userId, item, ct),
            "Transaction" => await UpsertTransactionAsync(userId, item, ct),
            "Category" => await UpsertCategoryAsync(userId, item, ct),
            "CreditCard" => await UpsertCreditCardAsync(userId, item, ct),
            "Budget" => await UpsertBudgetAsync(userId, item, ct),
            "Goal" => await UpsertGoalAsync(userId, item, ct),
            "RecurringTransaction" => await UpsertRecurringTransactionAsync(userId, item, ct),
            _ => Result<long>.Failure(Error.Business($"Unsupported entity for sync: {item.EntityName}"))
        };
    }
}
