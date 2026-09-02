using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Infrastructure.Persistence;

public sealed partial class AppDbContext
{
    private static void ConfigureBudget(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Budget>(e =>
        {
            e.ToTable("Budgets");
            e.Property(b => b.Limit).HasConversion(MoneyC);
            e.Property(b => b.Spent).HasConversion(MoneyC);
            e.HasIndex(b => new { b.UserId, b.CategoryId, b.Year });
        });
    }

    private static void ConfigureGoal(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FinancialGoal>(e =>
        {
            e.ToTable("FinancialGoals");
            e.Property(g => g.Name).HasMaxLength(120).IsRequired();
            e.Property(g => g.TargetAmount).HasConversion(MoneyC);
            e.Property(g => g.CurrentAmount).HasConversion(MoneyC);
            e.HasIndex(g => g.UserId);

            e.OwnsMany(g => g.Contributions, cb =>
            {
                cb.ToTable("GoalContributions");
                cb.Property(c => c.Amount).HasConversion(MoneyC);
                cb.Property(c => c.Note).HasMaxLength(500);
                cb.WithOwner().HasForeignKey("FinancialGoalId");
            });
        });
    }

    private static void ConfigureRecurring(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecurringTransaction>(e =>
        {
            e.ToTable("RecurringTransactions");
            e.Property(r => r.Description).HasMaxLength(200).IsRequired();
            e.Property(r => r.Amount).HasConversion(MoneyC);
            e.HasIndex(r => new { r.UserId, r.IsActive, r.NextExecution });
        });
    }

    private static void ConfigureSyncItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SyncItem>(e =>
        {
            e.ToTable("SyncItems");
            e.HasIndex(s => new { s.UserId, s.ClientOperationId }).IsUnique();
            e.HasIndex(s => new { s.UserId, s.Status, s.NextRetryAtUtc });
            e.HasIndex(s => s.EntityName);
        });
    }

    private static void ConfigureNotification(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(e =>
        {
            e.ToTable("Notifications");
            e.Property(n => n.Title).HasMaxLength(200).IsRequired();
            e.HasIndex(n => new { n.UserId, n.IsRead });
        });
    }

    private static void ConfigureAudit(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditEntry>(e =>
        {
            e.ToTable("AuditEntries");
            e.Property(a => a.EntityName).HasMaxLength(80).IsRequired();
            e.HasIndex(a => new { a.UserId, a.OccurredAtUtc });
        });
    }
}
