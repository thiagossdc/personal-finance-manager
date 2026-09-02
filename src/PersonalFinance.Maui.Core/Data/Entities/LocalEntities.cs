using SQLite;

namespace PersonalFinance.Maui.Core.Data.Entities;

/// <summary>
/// Entidade local de conta financeira. Espelha a entidade do domínio para persistência offline.
/// </summary>
public sealed class LocalAccount
{
    [PrimaryKey, AutoIncrement]
    public int LocalId { get; set; }
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Checking";
    public decimal InitialBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public string Currency { get; set; } = "BRL";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long Version { get; set; } = 1;
}

public sealed class LocalCategory
{
    [PrimaryKey, AutoIncrement]
    public int LocalId { get; set; }
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string Type { get; set; } = "Expense";
    public string? ParentId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long Version { get; set; } = 1;
}

public sealed class LocalTransaction
{
    [PrimaryKey, AutoIncrement]
    public int LocalId { get; set; }
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AccountId { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public string Type { get; set; } = "Expense";
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Completed";
    public string? Note { get; set; }
    public string? TransferId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long Version { get; set; } = 1;
}

public sealed class LocalTransfer
{
    [PrimaryKey, AutoIncrement]
    public int LocalId { get; set; }
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SourceAccountId { get; set; } = string.Empty;
    public string TargetAccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long Version { get; set; } = 1;
}


public sealed class LocalCreditCard
{
    [PrimaryKey, AutoIncrement]
    public int LocalId { get; set; }
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? LastFourDigits { get; set; }
    public decimal CreditLimit { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public decimal OpenAmount { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long Version { get; set; } = 1;
}

public sealed class LocalBudget
{
    [PrimaryKey, AutoIncrement]
    public int LocalId { get; set; }
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CategoryId { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public string Period { get; set; } = "Monthly";
    public int? Month { get; set; }
    public int? Year { get; set; }
    public decimal AlertThreshold { get; set; } = 0.8m;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long Version { get; set; } = 1;
}

public sealed class LocalGoal
{
    [PrimaryKey, AutoIncrement]
    public int LocalId { get; set; }
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateTime? Deadline { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long Version { get; set; } = 1;
}

public sealed class LocalRecurringTransaction
{
    [PrimaryKey, AutoIncrement]
    public int LocalId { get; set; }
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AccountId { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public string Type { get; set; } = "Expense";
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Frequency { get; set; } = "Monthly";
    public DateTime StartDate { get; set; }
    public DateTime? NextExecution { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long Version { get; set; } = 1;
}
