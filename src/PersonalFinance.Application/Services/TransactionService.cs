using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services.Mapping;
using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Application.Services;

public sealed partial class TransactionService : ServiceBase, ITransactionService
{
    private readonly BalanceCalculator _balance;

    public TransactionService(IAppDbContext db, ICurrentUser currentUser, IDateTime time, BalanceCalculator balance)
        : base(db, currentUser, time)
    {
        _balance = balance;
    }

    public async Task<Result<PaginatedList<TransactionDto>>> GetTransactionsAsync(
        Guid? accountId, Guid? categoryId, TransactionType? type, DateTime? from, DateTime? toDate,
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var query = Db.Transactions.Where(t => t.UserId == userId);

        if (accountId.HasValue)
        {
            query = query.Where(t => t.AccountId == accountId);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == categoryId);
        }

        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type);
        }

        if (from.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= from.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= toDate.Value.Date);
        }

        // Paginação + projeção traduzível (sem avaliação de dicionário em memória
        // dentro do SQL). Os nomes de categoria são resolvidos em uma única query
        // adicional apenas para as categorias presentes na página atual.
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var ordered = query.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.CreatedAtUtc);
        var totalCount = await ordered.CountAsync(ct);
        var rows = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.AccountId,
                t.CategoryId,
                t.Type,
                Amount = t.Amount.Amount,
                t.Description,
                t.TransactionDate,
                t.Status,
                t.Note,
                t.TransferId,
                t.InstallmentId
            })
            .ToListAsync(ct);

        var categoryIds = rows.Where(r => r.CategoryId.HasValue).Select(r => r.CategoryId!.Value).Distinct().ToList();
        var categoryNames = categoryIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await Db.Categories
                .Where(c => c.UserId == userId && categoryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var items = rows
            .Select(r => new TransactionDto(
                r.Id, r.AccountId, r.CategoryId,
                r.CategoryId.HasValue && categoryNames.TryGetValue(r.CategoryId.Value, out var name) ? name : string.Empty,
                r.Type, r.Amount, r.Description, r.TransactionDate, r.Status, r.Note, r.TransferId, r.InstallmentId))
            .ToList();

        var result = new PaginatedList<TransactionDto>(items, page, pageSize, totalCount);
        return Result<PaginatedList<TransactionDto>>.Success(result);
    }

    public async Task<Result<TransactionDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var t = await Db.Transactions.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (t is null)
        {
            return Error.NotFound("Transaction was not found.");
        }

        return Result<TransactionDto>.Success(t.ToDto(await GetCategoryNameAsync(userId, t.CategoryId, ct)));
    }

    public async Task<Result<TransactionDto>> CreateAsync(CreateTransactionRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var account = await Db.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId, ct);
        if (account is null)
        {
            return Error.NotFound("Account was not found.");
        }

        if (!await ValidateCategoryAsync(userId, request.CategoryId, ct))
        {
            return Error.Validation("Category is invalid for this user.");
        }

        var amount = Money.From(request.Amount, account.Currency);
        var createResult = request.Type == TransactionType.Income
            ? Transaction.CreateIncome(userId, request.AccountId, amount, request.Description, request.TransactionDate, request.CategoryId, request.Note)
            : Transaction.CreateExpense(userId, request.AccountId, amount, request.Description, request.TransactionDate, request.CategoryId, request.Note);

        if (createResult.IsFailure)
        {
            return createResult.Error!;
        }

        var transaction = createResult.Value!;
        Db.Transactions.Add(transaction);

        ApplyTags(transaction, request.Tags);

        // Atomicidade: transação + recálculo de saldo em uma única transação de banco.
        // Via execution strategy para compatibilidade com EnableRetryOnFailure (Postgres).
        return await TransactionalExecutor.ExecuteAsync(Db, async () =>
        {
            await Db.SaveChangesAsync(ct);
            await _balance.RecomputeAsync(account, ct);
            await Db.SaveChangesAsync(ct);

            return Result<TransactionDto>.Success(transaction.ToDto(await GetCategoryNameAsync(userId, transaction.CategoryId, ct)));
        }, ct);
    }
}
public sealed partial class TransactionService
{
    public async Task<Result<TransactionDto>> UpdateAsync(Guid id, UpdateTransactionRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var transaction = await Db.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
        if (transaction is null)
        {
            return Error.NotFound("Transaction was not found.");
        }

        if (transaction.TransferId.HasValue || transaction.InstallmentId.HasValue)
        {
            return Error.Business("This transaction is managed by a transfer or installment and cannot be edited directly.");
        }

        if (!await ValidateCategoryAsync(userId, request.CategoryId, ct))
        {
            return Error.Validation("Category is invalid for this user.");
        }

        var account = await Db.Accounts.FirstOrDefaultAsync(a => a.Id == transaction.AccountId, ct);
        var amount = Money.From(request.Amount, account?.Currency ?? "BRL");

        var updateResult = transaction.Update(request.TransactionDate, amount, request.Description, request.CategoryId, request.Note);
        if (updateResult.IsFailure)
        {
            return Result<TransactionDto>.Failure(updateResult.Error!);
        }

        return await TransactionalExecutor.ExecuteAsync(Db, async () =>
        {
            await Db.SaveChangesAsync(ct);
            if (account is not null)
            {
                await _balance.RecomputeAsync(account, ct);
                await Db.SaveChangesAsync(ct);
            }

            return Result<TransactionDto>.Success(transaction.ToDto(await GetCategoryNameAsync(userId, transaction.CategoryId, ct)));
        }, ct);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var transaction = await Db.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
        if (transaction is null)
        {
            return Error.NotFound("Transaction was not found.");
        }

        var account = await Db.Accounts.FirstOrDefaultAsync(a => a.Id == transaction.AccountId, ct);
        Db.Transactions.Remove(transaction);

        return await TransactionalExecutor.ExecuteAsync(Db, async () =>
        {
            await Db.SaveChangesAsync(ct);

            if (account is not null)
            {
                await _balance.RecomputeAsync(account, ct);
                await Db.SaveChangesAsync(ct);
            }

            return Result.Success();
        }, ct);
    }

    private async Task<string> GetCategoryNameAsync(Guid userId, Guid? categoryId, CancellationToken ct)
    {
        if (!categoryId.HasValue)
        {
            return string.Empty;
        }

        var name = await Db.Categories.Where(c => c.UserId == userId && c.Id == categoryId)
            .Select(c => c.Name).FirstOrDefaultAsync(ct);
        return name ?? string.Empty;
    }

    private Task<bool> ValidateCategoryAsync(Guid userId, Guid? categoryId, CancellationToken ct)
    {
        if (!categoryId.HasValue)
        {
            return Task.FromResult(true);
        }

        return Db.Categories.AnyAsync(c => c.Id == categoryId && c.UserId == userId, ct);
    }

    private void ApplyTags(Transaction transaction, IReadOnlyList<string>? tags)
    {
        if (tags is null)
        {
            return;
        }

        var userId = RequireUserId();
        foreach (var tagName in tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct())
        {
            var normalized = tagName.Trim().ToLowerInvariant();
            var existing = Db.Tags.Local.FirstOrDefault(t => t.Name == normalized)
                ?? Db.Tags.FirstOrDefault(t => t.Name == normalized);
            if (existing is not null)
            {
                transaction.AddTag(existing);
            }
            else
            {
                var tag = Tag.Create(userId, tagName).Value!;
                Db.Tags.Add(tag);
                transaction.AddTag(tag);
            }
        }
    }
}
