using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Abstractions;
using PersonalFinance.Application.Common;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Infrastructure.Persistence;

/// <summary>
/// Popula dados de demonstração em ambiente de desenvolvimento apenas (nunca dados reais).
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IAppDbContext db, IPasswordHasher passwordHasher, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(u => u.Email == "demo@finance.local", ct))
        {
            return;
        }

        var demoUser = User.Create("demo@finance.local", "Usuário Demonstração", passwordHasher.Hash("Demo@12345")).Value!;
        db.Users.Add(demoUser);

        var checking = Account.Create(demoUser.Id, "Conta Corrente", AccountType.Checking, Money.From(8500m)).Value!;
        var wallet = Account.Create(demoUser.Id, "Carteira", AccountType.Wallet, Money.From(300m)).Value!;
        db.Accounts.AddRange(checking, wallet);

        var food = Category.Create(demoUser.Id, "Alimentação", TransactionType.Expense, "🍔").Value!;
        var transport = Category.Create(demoUser.Id, "Transporte", TransactionType.Expense, "🚗").Value!;
        var housing = Category.Create(demoUser.Id, "Moradia", TransactionType.Expense, "🏠").Value!;
        var salary = Category.Create(demoUser.Id, "Salário", TransactionType.Income, "💼").Value!;
        db.Categories.AddRange(food, transport, housing, salary);

        var card = CreditCard.Create(demoUser.Id, "Cartão de Crédito", Money.From(5000m), 10, 17).Value!;
        db.CreditCards.Add(card);

        var income = Transaction.CreateIncome(demoUser.Id, checking.Id, Money.From(6500m), "Salário", DateTime.UtcNow.AddMonths(-1), salary.Id).Value!;
        db.Transactions.Add(income);

        await db.SaveChangesAsync(ct);
    }
}
