using System.Globalization;
using System.Text;
using System.Text.Json;
using PersonalFinance.Maui.Core.Data;
using PersonalFinance.Maui.Core.Data.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PersonalFinance.Maui.Core.Services;

public sealed class LocalFinanceService : ILocalFinanceService
{
    private static readonly CultureInfo PtBrCulture = new("pt-BR");
    private readonly LocalDbContext _localDb;

    public LocalFinanceService(LocalDbContext localDb)
    {
        _localDb = localDb;
        try
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }
        catch
        {
            // A licença só pode ser configurada uma vez por processo
        }
    }

    // ---- Contas ----
    public Task<List<LocalAccount>> GetAccountsAsync(CancellationToken ct = default) =>
        _localDb.GetAccountsAsync();

    public Task<LocalAccount?> GetAccountAsync(string id, CancellationToken ct = default) =>
        _localDb.GetAccountAsync(id);

    public async Task<LocalAccount> CreateAccountAsync(string name, string type, decimal initialBalance, string currency = "BRL", CancellationToken ct = default)
    {
        var account = new LocalAccount
        {
            Name = name.Trim(),
            Type = type,
            InitialBalance = initialBalance,
            CurrentBalance = initialBalance,
            Currency = currency.ToUpperInvariant(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        await _localDb.SaveAccountAsync(account);
        await EnqueueAsync("Account", account.Id, "Create", account, 1);
        return account;
    }

    public async Task UpdateAccountAsync(string id, string name, bool isActive, CancellationToken ct = default)
    {
        var account = await _localDb.GetAccountAsync(id);
        if (account is null) return;

        account.Name = name.Trim();
        account.IsActive = isActive;
        account.UpdatedAt = DateTime.UtcNow;
        account.Version++;

        await _localDb.SaveAccountAsync(account);
        await EnqueueAsync("Account", account.Id, "Update", account, account.Version);
    }

    public async Task DeleteAccountAsync(string id, CancellationToken ct = default)
    {
        var account = await _localDb.GetAccountAsync(id);
        if (account is null) return;

        account.IsActive = false;
        account.UpdatedAt = DateTime.UtcNow;
        account.Version++;

        await _localDb.SaveAccountAsync(account);
        await EnqueueAsync("Account", account.Id, "Delete", account, account.Version);
    }

    // ---- Categorias ----
    public Task<List<LocalCategory>> GetCategoriesAsync(CancellationToken ct = default) =>
        _localDb.GetCategoriesAsync();

    public async Task<LocalCategory> CreateCategoryAsync(string name, string type, string? icon = null, string? parentId = null, CancellationToken ct = default)
    {
        var category = new LocalCategory
        {
            Name = name.Trim(),
            Type = type,
            Icon = icon,
            ParentId = parentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        await _localDb.SaveCategoryAsync(category);
        await EnqueueAsync("Category", category.Id, "Create", category, 1);
        return category;
    }

    public async Task UpdateCategoryAsync(string id, string name, string? icon, bool isActive, CancellationToken ct = default)
    {
        var category = await _localDb.GetCategoryAsync(id);
        if (category is null) return;

        category.Name = name.Trim();
        category.Icon = icon;
        category.IsActive = isActive;
        category.UpdatedAt = DateTime.UtcNow;
        category.Version++;

        await _localDb.SaveCategoryAsync(category);
        await EnqueueAsync("Category", category.Id, "Update", category, category.Version);
    }

    public async Task DeleteCategoryAsync(string id, CancellationToken ct = default)
    {
        var category = await _localDb.GetCategoryAsync(id);
        if (category is null) return;

        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;
        category.Version++;

        await _localDb.SaveCategoryAsync(category);
        await EnqueueAsync("Category", category.Id, "Delete", category, category.Version);
    }

    // ---- Transações ----
    public Task<List<LocalTransaction>> GetTransactionsAsync(int limit = 100, CancellationToken ct = default) =>
        _localDb.GetTransactionsAsync(limit);

    public async Task<LocalTransaction> CreateTransactionAsync(
        string accountId,
        string type,
        decimal amount,
        string description,
        DateTime transactionDate,
        string? categoryId = null,
        string? note = null,
        CancellationToken ct = default)
    {
        var transaction = new LocalTransaction
        {
            AccountId = accountId,
            Type = type,
            Amount = amount,
            Description = description.Trim(),
            TransactionDate = transactionDate,
            CategoryId = categoryId,
            Note = note,
            Status = "Completed",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        await _localDb.SaveTransactionAsync(transaction);

        var account = await _localDb.GetAccountAsync(accountId);
        if (account is not null)
        {
            account.CurrentBalance += type == "Income" ? amount : -amount;
            account.UpdatedAt = DateTime.UtcNow;
            account.Version++;
            await _localDb.SaveAccountAsync(account);
        }

        await EnqueueAsync("Transaction", transaction.Id, "Create", transaction, 1);
        return transaction;
    }

    public async Task UpdateTransactionAsync(
        string id,
        decimal amount,
        string description,
        DateTime transactionDate,
        string? categoryId = null,
        string? note = null,
        CancellationToken ct = default)
    {
        var transaction = await _localDb.GetTransactionAsync(id);
        if (transaction is null) return;

        var oldAmount = transaction.Amount;
        var oldType = transaction.Type;

        transaction.Amount = amount;
        transaction.Description = description.Trim();
        transaction.TransactionDate = transactionDate;
        transaction.CategoryId = categoryId;
        transaction.Note = note;
        transaction.UpdatedAt = DateTime.UtcNow;
        transaction.Version++;

        await _localDb.SaveTransactionAsync(transaction);

        var account = await _localDb.GetAccountAsync(transaction.AccountId);
        if (account is not null)
        {
            var oldDelta = oldType == "Income" ? oldAmount : -oldAmount;
            var newDelta = transaction.Type == "Income" ? amount : -amount;
            account.CurrentBalance = account.CurrentBalance - oldDelta + newDelta;
            account.UpdatedAt = DateTime.UtcNow;
            account.Version++;
            await _localDb.SaveAccountAsync(account);
        }

        await EnqueueAsync("Transaction", transaction.Id, "Update", transaction, transaction.Version);
    }

    public async Task DeleteTransactionAsync(string id, CancellationToken ct = default)
    {
        var transaction = await _localDb.GetTransactionAsync(id);
        if (transaction is null) return;

        transaction.Status = "Canceled";
        transaction.UpdatedAt = DateTime.UtcNow;
        transaction.Version++;

        await _localDb.SaveTransactionAsync(transaction);

        var account = await _localDb.GetAccountAsync(transaction.AccountId);
        if (account is not null)
        {
            var delta = transaction.Type == "Income" ? transaction.Amount : -transaction.Amount;
            account.CurrentBalance -= delta;
            account.UpdatedAt = DateTime.UtcNow;
            account.Version++;
            await _localDb.SaveAccountAsync(account);
        }

        await EnqueueAsync("Transaction", transaction.Id, "Delete", transaction, transaction.Version);
    }

    // ---- Transferências ----
    public Task<List<LocalTransfer>> GetTransfersAsync(CancellationToken ct = default) =>
        _localDb.GetTransfersAsync();

    public async Task<LocalTransfer> CreateTransferAsync(
        string sourceAccountId,
        string targetAccountId,
        decimal amount,
        string description,
        DateTime? transferDate = null,
        CancellationToken ct = default)
    {
        var date = transferDate ?? DateTime.UtcNow;
        var transfer = new LocalTransfer
        {
            SourceAccountId = sourceAccountId,
            TargetAccountId = targetAccountId,
            Amount = amount,
            Description = description,
            TransferDate = date,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        await _localDb.SaveTransferAsync(transfer);

        var source = await _localDb.GetAccountAsync(sourceAccountId);
        var target = await _localDb.GetAccountAsync(targetAccountId);

        if (source is not null)
        {
            source.CurrentBalance -= amount;
            source.UpdatedAt = DateTime.UtcNow;
            source.Version++;
            await _localDb.SaveAccountAsync(source);
        }

        if (target is not null)
        {
            target.CurrentBalance += amount;
            target.UpdatedAt = DateTime.UtcNow;
            target.Version++;
            await _localDb.SaveAccountAsync(target);
        }

        await EnqueueAsync("Transfer", transfer.Id, "Create", transfer, 1);
        return transfer;
    }

    // ---- Cartões de Crédito ----
    public Task<List<LocalCreditCard>> GetCreditCardsAsync(CancellationToken ct = default) =>
        _localDb.GetCreditCardsAsync();

    public async Task<LocalCreditCard> CreateCreditCardAsync(
        string name,
        decimal creditLimit,
        int closingDay,
        int dueDay,
        string? lastFourDigits = null,
        CancellationToken ct = default)
    {
        var card = new LocalCreditCard
        {
            Name = name.Trim(),
            CreditLimit = creditLimit,
            ClosingDay = closingDay,
            DueDay = dueDay,
            LastFourDigits = lastFourDigits,
            OpenAmount = 0m,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        await _localDb.SaveCreditCardAsync(card);
        await EnqueueAsync("CreditCard", card.Id, "Create", card, 1);
        return card;
    }

    public async Task RecordCreditCardPurchaseAsync(
        string creditCardId,
        string description,
        decimal amount,
        int installmentCount = 1,
        string? categoryId = null,
        CancellationToken ct = default)
    {
        var card = await _localDb.GetCreditCardAsync(creditCardId);
        if (card is null) return;

        card.OpenAmount += amount;
        card.UpdatedAt = DateTime.UtcNow;
        card.Version++;
        await _localDb.SaveCreditCardAsync(card);

        var payload = new
        {
            CreditCardId = creditCardId,
            Description = description,
            Amount = amount,
            InstallmentCount = installmentCount,
            CategoryId = categoryId,
            PurchaseDate = DateTime.UtcNow
        };

        await EnqueueAsync("CreditCardPurchase", Guid.NewGuid().ToString(), "Create", payload, 1);
    }

    public async Task PayCreditCardAsync(string creditCardId, string sourceAccountId, decimal amount, CancellationToken ct = default)
    {
        var card = await _localDb.GetCreditCardAsync(creditCardId);
        var account = await _localDb.GetAccountAsync(sourceAccountId);

        if (card is not null)
        {
            card.OpenAmount = Math.Max(0, card.OpenAmount - amount);
            card.UpdatedAt = DateTime.UtcNow;
            card.Version++;
            await _localDb.SaveCreditCardAsync(card);
        }

        if (account is not null)
        {
            account.CurrentBalance -= amount;
            account.UpdatedAt = DateTime.UtcNow;
            account.Version++;
            await _localDb.SaveAccountAsync(account);
        }

        var payload = new
        {
            CreditCardId = creditCardId,
            SourceAccountId = sourceAccountId,
            Amount = amount,
            PaymentDate = DateTime.UtcNow
        };

        await EnqueueAsync("CreditCardPayment", Guid.NewGuid().ToString(), "Create", payload, 1);
    }

    // ---- Orçamentos ----
    public async Task<List<LocalBudgetWithProgress>> GetBudgetsAsync(int? month = null, int? year = null, CancellationToken ct = default)
    {
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var targetYear = year ?? DateTime.UtcNow.Year;

        var budgets = await _localDb.GetBudgetsAsync();
        var categories = await _localDb.GetCategoriesAsync();
        var transactions = await _localDb.GetTransactionsAsync(500);

        var categoryMap = categories.ToDictionary(c => c.Id, c => c.Name);

        var result = new List<LocalBudgetWithProgress>();
        foreach (var b in budgets)
        {
            var spent = transactions
                .Where(t => t.CategoryId == b.CategoryId
                    && t.Type == "Expense"
                    && t.Status != "Canceled"
                    && t.TransactionDate.Month == targetMonth
                    && t.TransactionDate.Year == targetYear)
                .Sum(t => t.Amount);

            var remaining = Math.Max(0, b.Limit - spent);
            var progress = b.Limit > 0 ? spent / b.Limit : 0m;
            var catName = categoryMap.TryGetValue(b.CategoryId, out var name) ? name : "Geral";

            result.Add(new LocalBudgetWithProgress(
                b.Id,
                b.CategoryId,
                catName,
                b.Limit,
                spent,
                remaining,
                progress,
                b.AlertThreshold,
                progress >= b.AlertThreshold,
                spent > b.Limit));
        }

        return result;
    }

    public async Task<LocalBudget> CreateBudgetAsync(
        string categoryId,
        decimal limit,
        string period = "Monthly",
        int? month = null,
        int? year = null,
        decimal alertThreshold = 0.8m,
        CancellationToken ct = default)
    {
        var budget = new LocalBudget
        {
            CategoryId = categoryId,
            Limit = limit,
            Period = period,
            Month = month ?? DateTime.UtcNow.Month,
            Year = year ?? DateTime.UtcNow.Year,
            AlertThreshold = alertThreshold,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        await _localDb.SaveBudgetAsync(budget);
        await EnqueueAsync("Budget", budget.Id, "Create", budget, 1);
        return budget;
    }

    public async Task UpdateBudgetLimitAsync(string id, decimal limit, CancellationToken ct = default)
    {
        var budget = await _localDb.GetBudgetAsync(id);
        if (budget is null) return;

        budget.Limit = limit;
        budget.UpdatedAt = DateTime.UtcNow;
        budget.Version++;

        await _localDb.SaveBudgetAsync(budget);
        await EnqueueAsync("Budget", budget.Id, "Update", budget, budget.Version);
    }

    // ---- Metas ----
    public Task<List<LocalGoal>> GetGoalsAsync(CancellationToken ct = default) =>
        _localDb.GetGoalsAsync();

    public async Task<LocalGoal> CreateGoalAsync(string name, decimal targetAmount, DateTime? deadline = null, CancellationToken ct = default)
    {
        var goal = new LocalGoal
        {
            Name = name.Trim(),
            TargetAmount = targetAmount,
            CurrentAmount = 0m,
            Deadline = deadline,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        await _localDb.SaveGoalAsync(goal);
        await EnqueueAsync("Goal", goal.Id, "Create", goal, 1);
        return goal;
    }

    public async Task ContributeToGoalAsync(string goalId, decimal amount, string? sourceAccountId = null, string? note = null, CancellationToken ct = default)
    {
        var goal = await _localDb.GetGoalAsync(goalId);
        if (goal is null) return;

        goal.CurrentAmount += amount;
        if (goal.CurrentAmount >= goal.TargetAmount)
        {
            goal.Status = "Completed";
        }
        goal.UpdatedAt = DateTime.UtcNow;
        goal.Version++;
        await _localDb.SaveGoalAsync(goal);

        if (!string.IsNullOrEmpty(sourceAccountId))
        {
            var account = await _localDb.GetAccountAsync(sourceAccountId);
            if (account is not null)
            {
                account.CurrentBalance -= amount;
                account.UpdatedAt = DateTime.UtcNow;
                account.Version++;
                await _localDb.SaveAccountAsync(account);
            }
        }

        var payload = new
        {
            GoalId = goalId,
            Amount = amount,
            SourceAccountId = sourceAccountId,
            Note = note,
            ContributedAtUtc = DateTime.UtcNow
        };

        await EnqueueAsync("GoalContribution", Guid.NewGuid().ToString(), "Create", payload, 1);
    }

    // ---- Transações Recorrentes ----
    public Task<List<LocalRecurringTransaction>> GetRecurringTransactionsAsync(CancellationToken ct = default) =>
        _localDb.GetRecurringTransactionsAsync();

    public async Task<LocalRecurringTransaction> CreateRecurringTransactionAsync(
        string accountId,
        string type,
        decimal amount,
        string description,
        string frequency,
        DateTime startDate,
        string? categoryId = null,
        CancellationToken ct = default)
    {
        var recurring = new LocalRecurringTransaction
        {
            AccountId = accountId,
            Type = type,
            Amount = amount,
            Description = description.Trim(),
            Frequency = frequency,
            StartDate = startDate,
            NextExecution = startDate,
            CategoryId = categoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        await _localDb.SaveRecurringTransactionAsync(recurring);
        await EnqueueAsync("RecurringTransaction", recurring.Id, "Create", recurring, 1);
        return recurring;
    }

    public async Task<int> ExecuteDueRecurringTransactionsAsync(CancellationToken ct = default)
    {
        var recurrings = await _localDb.GetRecurringTransactionsAsync();
        var now = DateTime.UtcNow;
        var executed = 0;

        foreach (var item in recurrings)
        {
            if (item.NextExecution.HasValue && item.NextExecution.Value <= now)
            {
                await CreateTransactionAsync(
                    item.AccountId,
                    item.Type,
                    item.Amount,
                    $"[Recorrente] {item.Description}",
                    item.NextExecution.Value,
                    item.CategoryId,
                    $"Gerado automaticamente em {now:dd/MM/yyyy}",
                    ct);

                item.NextExecution = CalculateNextDate(item.NextExecution.Value, item.Frequency);
                item.UpdatedAt = DateTime.UtcNow;
                item.Version++;
                await _localDb.SaveRecurringTransactionAsync(item);
                executed++;
            }
        }

        return executed;
    }

    public async Task DeactivateRecurringTransactionAsync(string id, CancellationToken ct = default)
    {
        var recurring = await _localDb.GetRecurringTransactionAsync(id);
        if (recurring is null) return;

        recurring.IsActive = false;
        recurring.UpdatedAt = DateTime.UtcNow;
        recurring.Version++;
        await _localDb.SaveRecurringTransactionAsync(recurring);
        await EnqueueAsync("RecurringTransaction", recurring.Id, "Update", recurring, recurring.Version);
    }

    private static DateTime CalculateNextDate(DateTime current, string frequency) =>
        frequency.ToLowerInvariant() switch
        {
            "weekly" or "semanal" => current.AddDays(7),
            "biweekly" or "quinzenal" => current.AddDays(14),
            "yearly" or "anual" => current.AddYears(1),
            _ => current.AddMonths(1) // padrão: mensal
        };

    // ---- Relatórios e Exportação ----
    public async Task<LocalMonthlyReport> GetMonthlyReportAsync(int month, int year, CancellationToken ct = default)
    {
        var transactions = await _localDb.GetTransactionsAsync(1000);
        var categories = await _localDb.GetCategoriesAsync();
        var categoryMap = categories.ToDictionary(c => c.Id, c => c.Name);

        var monthTxns = transactions
            .Where(t => t.TransactionDate.Month == month && t.TransactionDate.Year == year && t.Status != "Canceled")
            .ToList();

        var income = monthTxns.Where(t => t.Type == "Income").Sum(t => t.Amount);
        var expense = monthTxns.Where(t => t.Type == "Expense").Sum(t => t.Amount);
        var net = income - expense;
        var savingsRate = income > 0 ? Math.Max(0, net) / income : 0m;

        var catGroups = monthTxns
            .Where(t => t.Type == "Expense")
            .GroupBy(t => t.CategoryId ?? "Outros")
            .Select(g =>
            {
                var name = categoryMap.TryGetValue(g.Key, out var n) ? n : "Outros";
                var total = g.Sum(x => x.Amount);
                var pct = expense > 0 ? total / expense : 0m;
                return new CategoryExpenseSummary(name, total, pct);
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        var daily = monthTxns
            .Where(t => t.Type == "Expense")
            .GroupBy(t => t.TransactionDate.Day)
            .Select(g => new DailyExpenseSummary(g.Key, g.Sum(x => x.Amount)))
            .OrderBy(d => d.Day)
            .ToList();

        return new LocalMonthlyReport(month, year, income, expense, net, savingsRate, catGroups, daily);
    }

    public async Task<byte[]> ExportReportPdfAsync(int month, int year, CancellationToken ct = default)
    {
        var report = await GetMonthlyReportAsync(month, year, ct);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Personal Finance Manager").FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text($"Relatório Financeiro Mensal — {month:D2}/{year}").FontSize(13).FontColor(Colors.Grey.Darken1);
                    });
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    // Cartões de resumo
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                        {
                            c.Item().Text("Receitas").FontSize(10).FontColor(Colors.Grey.Medium);
                            c.Item().Text(report.TotalIncome.ToString("C", PtBrCulture)).FontSize(14).Bold().FontColor(Colors.Green.Darken1);
                        });

                        r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                        {
                            c.Item().Text("Despesas").FontSize(10).FontColor(Colors.Grey.Medium);
                            c.Item().Text(report.TotalExpense.ToString("C", PtBrCulture)).FontSize(14).Bold().FontColor(Colors.Red.Darken1);
                        });

                        r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                        {
                            c.Item().Text("Economia Líquida").FontSize(10).FontColor(Colors.Grey.Medium);
                            c.Item().Text(report.NetSavings.ToString("C", PtBrCulture)).FontSize(14).Bold().FontColor(report.NetSavings >= 0 ? Colors.Blue.Darken1 : Colors.Red.Darken1);
                        });
                    });

                    col.Item().PaddingTop(20).Text("Despesas por Categoria").FontSize(14).Bold();

                    // Tabela
                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Categoria").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Valor").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("% do Total").Bold();
                        });

                        foreach (var cat in report.CategoryExpenses)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(cat.CategoryName);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(cat.TotalAmount.ToString("C", PtBrCulture));
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{cat.Percentage:P1}");
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Gerado pelo Personal Finance Manager em ");
                    x.Span(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm", PtBrCulture));
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<string> ExportTransactionsCsvAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
    {
        var transactions = await _localDb.GetTransactionsAsync(1000);
        var categories = await _localDb.GetCategoriesAsync();
        var accounts = await _localDb.GetAccountsAsync();

        var catMap = categories.ToDictionary(c => c.Id, c => c.Name);
        var accMap = accounts.ToDictionary(a => a.Id, a => a.Name);

        var query = transactions.AsEnumerable();
        if (fromDate.HasValue) query = query.Where(t => t.TransactionDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(t => t.TransactionDate <= toDate.Value);

        var sb = new StringBuilder();
        sb.AppendLine("ID,Data,Conta,Tipo,Categoria,Valor,Descricao,Status,Observacao");

        foreach (var t in query.OrderByDescending(x => x.TransactionDate))
        {
            var acc = accMap.TryGetValue(t.AccountId, out var aName) ? aName : t.AccountId;
            var cat = (t.CategoryId != null && catMap.TryGetValue(t.CategoryId, out var cName)) ? cName : "";
            var amountStr = t.Amount.ToString("F2", CultureInfo.InvariantCulture);
            var desc = EscapeCsv(t.Description);
            var note = EscapeCsv(t.Note ?? "");

            sb.AppendLine(CultureInfo.InvariantCulture, $"{t.Id},{t.TransactionDate:yyyy-MM-dd},{EscapeCsv(acc)},{t.Type},{EscapeCsv(cat)},{amountStr},{desc},{t.Status},{note}");
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    private async Task EnqueueAsync(string entityName, string entityId, string changeType, object payload, long baseVersion)
    {
        await _localDb.EnqueueSyncAsync(new SyncQueueItem
        {
            EntityName = entityName,
            EntityId = entityId,
            ChangeType = changeType,
            Payload = JsonSerializer.Serialize(payload),
            BaseVersion = baseVersion,
            SyncStatus = SyncStatusNames.Pending,
            CreatedAt = DateTime.UtcNow
        });
    }
}

