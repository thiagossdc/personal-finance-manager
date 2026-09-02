using Microsoft.EntityFrameworkCore;
using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Infrastructure.Persistence;

public sealed partial class AppDbContext
{
    private static readonly MoneyConverter MoneyC = new();

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(320).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.DisplayName).HasMaxLength(120).IsRequired();
        });
    }

    private static void ConfigureAccount(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(e =>
        {
            e.ToTable("Accounts");
            e.Property(a => a.Name).HasMaxLength(120).IsRequired();
            e.Property(a => a.InitialBalance).HasConversion(MoneyC);
            e.Property(a => a.CurrentBalance).HasConversion(MoneyC);
            e.HasIndex(a => a.UserId);
            e.HasIndex(a => a.UpdatedAtUtc);
            e.HasIndex(a => new { a.UserId, a.IsActive });
        });
    }

    private static void ConfigureCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.Property(c => c.Name).HasMaxLength(80).IsRequired();
            e.HasIndex(c => c.UserId);
            e.HasIndex(c => c.ParentId);
        });
    }

    private static void ConfigureTransaction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(e =>
        {
            e.ToTable("Transactions");
            e.Property(t => t.Description).HasMaxLength(200).IsRequired();
            e.Property(t => t.Amount).HasConversion(MoneyC);
            e.HasOne(t => t.Category).WithMany().HasForeignKey(t => t.CategoryId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(t => new { t.UserId, t.TransactionDate });
            e.HasIndex(t => t.AccountId);
            e.HasIndex(t => t.CategoryId);
            e.HasIndex(t => new { t.UserId, t.UpdatedAtUtc });
            e.HasIndex(t => t.Type);
        });
    }

    private static void ConfigureTransfer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transfer>(e =>
        {
            e.ToTable("Transfers");
            e.Property(t => t.Description).HasMaxLength(200).IsRequired();
            e.Property(t => t.Amount).HasConversion(MoneyC);
            e.HasIndex(t => new { t.UserId, t.TransferDate });
            e.HasIndex(t => t.SourceAccountId);
            e.HasIndex(t => t.TargetAccountId);
        });
    }

    private static void ConfigureCreditCard(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CreditCard>(e =>
        {
            e.ToTable("CreditCards");
            e.Property(c => c.Name).HasMaxLength(120).IsRequired();
            e.Property(c => c.CreditLimit).HasConversion(MoneyC);
            e.HasIndex(c => c.UserId);
        });

        modelBuilder.Entity<CreditCardTransaction>(e =>
        {
            e.ToTable("CreditCardTransactions");
            e.Property(t => t.Description).HasMaxLength(200).IsRequired();
            e.Property(t => t.Amount).HasConversion(MoneyC);
            e.HasOne<CreditCard>().WithMany(c => c.Transactions).HasForeignKey(t => t.CreditCardId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(t => t.CreditCardId);
        });

        modelBuilder.Entity<Installment>(e =>
        {
            e.ToTable("Installments");
            e.Property(i => i.Amount).HasConversion(MoneyC);
            e.HasOne<CreditCardTransaction>().WithMany(t => t.Installments).HasForeignKey(i => i.CreditCardTransactionId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(i => new { i.CreditCardTransactionId, i.Status });
            e.HasIndex(i => i.DueDate);
        });
    }
}
