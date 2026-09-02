using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common;
using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Infrastructure.Persistence;

public sealed partial class AppDbContext : DbContext, IAppDbContext
{
    private readonly VersionedEntityInterceptor _versioning;

    public AppDbContext(DbContextOptions<AppDbContext> options, VersionedEntityInterceptor versioning)
        : base(options)
    {
        _versioning = versioning;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<CreditCardTransaction> CreditCardTransactions => Set<CreditCardTransaction>();
    public DbSet<Installment> Installments => Set<Installment>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<FinancialGoal> FinancialGoals => Set<FinancialGoal>();
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<SyncItem> SyncItems => Set<SyncItem>();

    public new Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => base.SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ConfigureUser(modelBuilder);
        ConfigureAccount(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureTransaction(modelBuilder);
        ConfigureTransfer(modelBuilder);
        ConfigureCreditCard(modelBuilder);
        ConfigureBudget(modelBuilder);
        ConfigureGoal(modelBuilder);
        ConfigureRecurring(modelBuilder);
        ConfigureSyncItem(modelBuilder);
        ConfigureNotification(modelBuilder);
        ConfigureAudit(modelBuilder);
        IgnoreDomainEvents(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void IgnoreDomainEvents(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Ignore(nameof(Entity.DomainEvents));
            }
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            throw new InvalidOperationException("AppDbContext must be configured with a provider.");
        }

        optionsBuilder.AddInterceptors(_versioning);
        base.OnConfiguring(optionsBuilder);
    }
}
