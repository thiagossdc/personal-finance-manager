namespace PersonalFinance.Application.Common;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Executa um bloco de trabalho dentro de uma transação de banco, de forma
/// compatível com estratégias de execução com retry (ex.: Npgsql
/// NpgsqlRetryingExecutionStrategy, ativado por EnableRetryOnFailure).
/// BeginTransactionAsync direto falha nessas estratégias; aqui o bloco é
/// executado via execution strategy, que reexecuta o bloco inteiro
/// (transação incluída) em caso de falha transitória.
/// </summary>
public static class TransactionalExecutor
{
    public static Task<T> ExecuteAsync<T>(IAppDbContext db, Func<Task<T>> work, CancellationToken ct = default)
    {
        var dbContext = (DbContext)db;
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(() => RunInTransactionAsync(dbContext, work, ct));
    }

    public static Task ExecuteAsync(IAppDbContext db, Func<Task> work, CancellationToken ct = default) =>
        ExecuteAsync<object?>(db, async () =>
        {
            await work();
            return null;
        }, ct);

    private static async Task<T> RunInTransactionAsync<T>(DbContext dbContext, Func<Task<T>> work, CancellationToken ct)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await work();
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await dbContext.Database.RollbackTransactionAsync(ct);
            throw;
        }
    }
}
