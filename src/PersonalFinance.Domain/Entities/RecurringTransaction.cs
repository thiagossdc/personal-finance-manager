using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Domain.Entities;

/// <summary>
/// Transação recorrente (mensal, semanal, quinzenal, anual, customizada).
/// Não cria registros futuros indefinidamente; gera a próxima execução sob demanda e de forma idempotente.
/// </summary>
public class RecurringTransaction : Entity, IVersioned
{
    public Guid UserId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public string Description { get; private set; } = string.Empty;
    public RecurrenceFrequency Frequency { get; private set; }
    public int Interval { get; private set; } = 1;
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public int? CustomIntervalDays { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? LastExecutedAt { get; private set; }
    public DateTime? NextExecution { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    private RecurringTransaction() { } // EF Core

    public static Result<RecurringTransaction> Create(
        Guid userId,
        Guid accountId,
        TransactionType type,
        Money amount,
        string description,
        RecurrenceFrequency frequency,
        DateTime startDate,
        Guid? categoryId = null,
        int interval = 1,
        DateTime? endDate = null)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return Error.Validation("Description is required.");
        }

        if (amount.Amount <= 0m)
        {
            return Error.Validation("Amount must be positive.");
        }

        if (interval < 1)
        {
            return Error.Validation("Interval must be at least 1.");
        }

        var next = CalculateNext(startDate, startDate.Date, frequency, interval);
        var now = DateTime.UtcNow;
        return Result<RecurringTransaction>.Success(new RecurringTransaction
        {
            UserId = userId,
            AccountId = accountId,
            CategoryId = categoryId,
            Type = type,
            Amount = Money.From(Math.Abs(amount.Amount), amount.Currency),
            Description = description.Trim(),
            Frequency = frequency,
            Interval = interval,
            StartDate = startDate.Date,
            EndDate = endDate?.Date,
            NextExecution = next,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1
        });
    }

    public void BumpVersion() => Version++;
    public override void AssignClientId(Guid id) => Id = id;

    /// <summary>
    /// Avança a recorrência de forma idempotente: retorna a data da próxima execução após <paramref name="afterDate"/>.
    /// Cada chamada produz exatamente uma próxima execução, evitando acúmulo de registros futuros.
    /// </summary>
    public static DateTime CalculateNext(DateTime from, DateTime afterDate, RecurrenceFrequency frequency, int interval)
    {
        var baseDate = new DateTime(from.Year, from.Month, from.Day, 0, 0, 0, DateTimeKind.Utc);
        var target = afterDate.Date;
        var iterations = 0;

        // Limite de segurança para evitar loops infinitos.
        while (baseDate <= target && iterations < 10_000)
        {
            baseDate = AddInterval(baseDate, frequency, interval);
            iterations++;
        }

        return baseDate;
    }

    private static DateTime AddInterval(DateTime date, RecurrenceFrequency frequency, int interval) => frequency switch
    {
        RecurrenceFrequency.Weekly => date.AddDays(7 * interval),
        RecurrenceFrequency.Biweekly => date.AddDays(14 * interval),
        RecurrenceFrequency.Monthly => date.AddMonths(interval),
        RecurrenceFrequency.Quarterly => date.AddMonths(3 * interval),
        RecurrenceFrequency.Yearly => date.AddYears(interval),
        _ => date.AddDays(interval)
    };

    public bool IsDue(DateTime today) => NextExecution.HasValue && NextExecution.Value.Date <= today.Date && IsActive;

    /// <summary>Registra a execução e aponta para a próxima ocorrência.</summary>
    public void AdvanceExecution()
    {
        LastExecutedAt = DateTime.UtcNow;
        var from = NextExecution ?? StartDate;
        NextExecution = CalculateNext(StartDate, from.Date, Frequency, Interval);

        if (EndDate.HasValue && NextExecution.Value.Date > EndDate.Value.Date)
        {
            IsActive = false;
        }
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
