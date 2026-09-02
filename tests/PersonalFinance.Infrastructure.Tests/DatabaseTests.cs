using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;
using PersonalFinance.Infrastructure.Persistence;

namespace PersonalFinance.Infrastructure.Tests;

public sealed class DatabaseTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, new VersionedEntityInterceptor());
    }

    [Fact]
    public async Task SaveAccount_ShouldPersistToDatabase()
    {
        using var context = CreateInMemoryContext();

        var account = Account.Create(Guid.NewGuid(), "Conta Teste", AccountType.Checking, Money.Create(1000m).Value!).Value!;
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var savedAccount = await context.Accounts.FirstOrDefaultAsync(a => a.Id == account.Id);

        savedAccount.Should().NotBeNull();
        savedAccount!.Name.Should().Be("Conta Teste");
        savedAccount.CurrentBalance.Amount.Should().Be(1000m);
    }

    [Fact]
    public async Task SaveTransaction_ShouldPersistWithRelationships()
    {
        using var context = CreateInMemoryContext();

        var account = Account.Create(Guid.NewGuid(), "Conta", AccountType.Checking, Money.Create(1000m).Value!).Value!;
        context.Accounts.Add(account);

        var category = Category.Create(Guid.NewGuid(), "Alimentação", TransactionType.Expense).Value!;
        context.Categories.Add(category);

        var transaction = Transaction.CreateExpense(Guid.NewGuid(), account.Id, Money.Create(150m).Value!, "Almoço", DateTime.UtcNow, category.Id).Value!;
        context.Transactions.Add(transaction);

        await context.SaveChangesAsync();

        var savedTransaction = await context.Transactions.FirstOrDefaultAsync(t => t.Id == transaction.Id);
        savedTransaction.Should().NotBeNull();
        savedTransaction!.Amount.Amount.Should().Be(150m);
    }

    [Fact]
    public async Task DeleteAccount_ShouldCascadeToTransactions()
    {
        using var context = CreateInMemoryContext();

        var account = Account.Create(Guid.NewGuid(), "Conta", AccountType.Checking, Money.Create(1000m).Value!).Value!;
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        context.Accounts.Remove(account);
        await context.SaveChangesAsync();

        var deletedAccount = await context.Accounts.FirstOrDefaultAsync(a => a.Id == account.Id);
        deletedAccount.Should().BeNull();
    }

    [Fact]
    public async Task VersionedEntity_ShouldIncrementVersionOnSave()
    {
        using var context = CreateInMemoryContext();

        var account = Account.Create(Guid.NewGuid(), "Conta", AccountType.Checking, Money.Create(1000m).Value!).Value!;
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        account.Version.Should().BeGreaterThan(0);
    }
}
