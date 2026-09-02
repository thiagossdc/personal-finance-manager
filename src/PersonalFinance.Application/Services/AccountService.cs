using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services.Mapping;
using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Application.Services;

public sealed class AccountService : ServiceBase, IAccountService
{
    private readonly BalanceCalculator _balance;

    public AccountService(IAppDbContext db, ICurrentUser currentUser, IDateTime time, BalanceCalculator balance)
        : base(db, currentUser, time)
    {
        _balance = balance;
    }

    public async Task<Result<PaginatedList<AccountDto>>> GetAccountsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var query = Db.Accounts.Where(a => a.UserId == userId).OrderBy(a => a.Name);
        var result = await PaginatedList<AccountDto>.CreateAsync(
            query.Select(a => new AccountDto(a.Id, a.Name, a.Type, a.InitialBalance.Amount, a.CurrentBalance.Amount, a.Currency, a.IsActive)),
            page, pageSize, ct);
        return Result<PaginatedList<AccountDto>>.Success(result);
    }

    public async Task<Result<AccountDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var account = await Db.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
        if (account is null)
        {
            return Error.NotFound("Account was not found.");
        }

        return Result<AccountDto>.Success(account.ToDto());
    }

    public async Task<Result<AccountDto>> CreateAsync(CreateAccountRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var moneyResult = Money.Create(request.InitialBalance, request.Currency);
        if (moneyResult.IsFailure)
        {
            return moneyResult.Error!;
        }

        var createResult = Account.Create(userId, request.Name, request.Type, moneyResult.Value!, request.Currency);
        if (createResult.IsFailure)
        {
            return createResult.Error!;
        }

        Db.Accounts.Add(createResult.Value!);
        await Db.SaveChangesAsync(ct);
        return Result<AccountDto>.Success(createResult.Value!.ToDto());
    }

    public async Task<Result<AccountDto>> UpdateAsync(Guid id, UpdateAccountRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var account = await Db.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
        if (account is null)
        {
            return Error.NotFound("Account was not found.");
        }

        var rename = account.Rename(request.Name);
        if (rename.IsFailure)
        {
            return Result<AccountDto>.Failure(rename.Error!);
        }

        if (request.IsActive)
        {
            account.Activate();
        }
        else
        {
            account.Deactivate();
        }

        await Db.SaveChangesAsync(ct);
        return Result<AccountDto>.Success(account.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var account = await Db.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
        if (account is null)
        {
            return Error.NotFound("Account was not found.");
        }

        var hasTransactions = await Db.Transactions.AnyAsync(t => t.AccountId == id, ct);
        if (hasTransactions)
        {
            // Estratégia segura: desativa em vez de remover, preservando o histórico.
            account.Deactivate();
        }
        else
        {
            Db.Accounts.Remove(account);
        }

        await Db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
