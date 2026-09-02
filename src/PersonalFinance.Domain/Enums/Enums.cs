namespace PersonalFinance.Domain.Enums;

public enum TransactionType
{
    Expense = 0,
    Income = 1,
    Transfer = 2
}

public enum TransactionStatus
{
    Pending = 0,
    Confirmed = 1,
    Canceled = 2
}

public enum AccountType
{
    Checking = 0,
    Savings = 1,
    Wallet = 2,
    Investment = 3,
    Other = 4
}

public enum CreditCardStatus
{
    Active = 0,
    Closed = 1
}

public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Overdue = 2,
    Canceled = 3
}

public enum SyncStatus
{
    Pending = 0,
    InProgress = 1,
    Synced = 2,
    Failed = 3
}

public enum SyncChangeType
{
    Create = 0,
    Update = 1,
    Delete = 2
}

public enum RecurrenceFrequency
{
    Weekly = 0,
    Biweekly = 1,
    Monthly = 2,
    Quarterly = 3,
    Yearly = 4,
    Custom = 5
}

public enum GoalStatus
{
    Active = 0,
    Achieved = 1,
    Expired = 2,
    Cancelled = 3
}

public enum NotificationType
{
    BillDue = 0,
    BudgetThreshold = 1,
    GoalReminder = 2,
    CreditCardClosing = 3,
    System = 4
}

public enum BudgetPeriod
{
    Monthly = 0,
    Yearly = 1
}

public enum AuditAction
{
    Created = 0,
    Updated = 1,
    Deleted = 2
}
