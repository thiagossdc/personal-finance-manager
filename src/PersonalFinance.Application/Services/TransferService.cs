using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services.Mapping;
using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Application.Services;

public sealed class TransferService : ServiceBase, ITransferService
{
    private readonly BalanceCalculator _balance;

    public TransferService(IAppDbContext db, ICurrentUser currentUser, IDateTime time, BalanceCalculator balance)
        : base(db, currentUser, time)
    {
        _balance = balance;
    }

    public async Task<Result<TransferDto>> CreateAsync(CreateTransferRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var source = await Db.Accounts.FirstOrDefaultAsync(a => a.Id == request.SourceAccountId && a.UserId == userId, ct);
        var target = await Db.Accounts.FirstOrDefaultAsync(a => a.Id == request.TargetAccountId && a.UserId == userId, ct);

        if (source is null || target is null)
        {
            return Error.NotFound("One of the accounts was not found.");
        }

        var createResult = Transfer.Create(userId, source.Id, target.Id, Money.From(request.Amount, source.Currency), request.TransferDate, request.Description);
        if (createResult.IsFailure)
        {
            return createResult.Error!;
        }

        var transfer = createResult.Value!;
        Db.Transfers.Add(transfer);

        // Atomicidade: a transferência e o recálculo de saldo de ambas as contas
        // são persistidos em uma única transação — nunca deixa estado parcial
        // (débito sem crédito ou vice-versa). Via execution strategy para
        // compatibilidade com EnableRetryOnFailure (Postgres).
        return await TransactionalExecutor.ExecuteAsync(Db, async () =>
        {
            await Db.SaveChangesAsync(ct);
            await _balance.RecomputeAsync(source, ct);
            await _balance.RecomputeAsync(target, ct);
            await Db.SaveChangesAsync(ct);

            return Result<TransferDto>.Success(transfer.ToDto());
        }, ct);
    }

    public async Task<Result<PaginatedList<TransferDto>>> GetAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var query = Db.Transfers.Where(t => t.UserId == userId).OrderByDescending(t => t.TransferDate);
        var result = await PaginatedList<TransferDto>.CreateAsync(
            query.Select(t => new TransferDto(t.Id, t.SourceAccountId, t.TargetAccountId, t.Amount.Amount, t.TransferDate, t.Description)),
            page, pageSize, ct);
        return Result<PaginatedList<TransferDto>>.Success(result);
    }
}
