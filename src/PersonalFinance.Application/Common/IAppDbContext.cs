using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Application.Common;

/// <summary>
/// Abstração do contexto de persistência consumida pela camada de aplicação.
/// A camada de infraestrutura fornece a implementação (EF Core sobre PostgreSQL/SQLite).
/// Manter a aplicação dependendo apenas desta interface evita acoplamento ao EF na UI e nos testes.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Account> Accounts { get; }
    DbSet<Category> Categories { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<Transfer> Transfers { get; }
    DbSet<CreditCard> CreditCards { get; }
    DbSet<CreditCardTransaction> CreditCardTransactions { get; }
    DbSet<Installment> Installments { get; }
    DbSet<Budget> Budgets { get; }
    DbSet<FinancialGoal> FinancialGoals { get; }
    DbSet<RecurringTransaction> RecurringTransactions { get; }
    DbSet<Tag> Tags { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<AuditEntry> AuditEntries { get; }
    DbSet<SyncItem> SyncItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
