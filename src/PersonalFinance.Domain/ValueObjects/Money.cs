using System.Globalization;
using PersonalFinance.Domain.Common;

namespace PersonalFinance.Domain.ValueObjects;

/// <summary>
/// Representa um valor monetário. Nunca utilize <see cref="double"/> ou <see cref="float"/> para dinheiro.
/// Suporta soma, subtração e comparação preservando precisão decimal.
/// </summary>
public sealed class Money : ValueObject, IComparable<Money>
{
    public const string DefaultCurrency = "BRL";

    public string Currency { get; }
    public decimal Amount { get; }

    private Money(string currency, decimal amount)
    {
        Currency = currency;
        Amount = amount;
    }

    /// <summary>Valor zero na moeda informada.</summary>
    public static Money Zero(string currency = DefaultCurrency) => new(currency, 0m);

    /// <summary>
    /// Cria um <see cref="Money"/>. Constrói <paramref name="amount"/> como negativo quando
    /// <paramref name="isNegative"/> for true (útil para despesas).
    /// </summary>
    public static Result<Money> Create(decimal amount, string? currency = null, bool isNegative = false)
    {
        var currencyCode = NormalizeCurrency(currency);
        if (amount < 0)
        {
            return Error.Validation("Money amount must be non-negative.");
        }

        var signed = isNegative ? -amount : amount;
        return Result<Money>.Success(new Money(currencyCode, signed));
    }

    /// <summary>Cria sem validação/resultado — apenas para uso interno e serialização determinística.</summary>
    public static Money From(decimal amount, string currency = DefaultCurrency) => new(currency, amount);

    private static string NormalizeCurrency(string? currency)
    {
        var code = string.IsNullOrWhiteSpace(currency) ? DefaultCurrency : currency.Trim().ToUpperInvariant();
        if (code.Length != 3 || code.Any(c => !char.IsLetter(c)))
        {
            throw new ArgumentException("Currency must be a valid 3-letter ISO code.", nameof(currency));
        }

        return code;
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Currency, Amount + other.Amount);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Currency, Amount - other.Amount);
    }

    public Money NetAbs() => new(Currency, Math.Abs(Amount));

    public bool IsZero => Amount == 0m;

    public bool IsPositive => Amount > 0m;

    public bool IsNegative => Amount < 0m;

    /// <summary>
    /// Arredonda para a quantidade de casas decimais informada usando arredondamento bancário.
    /// </summary>
    public Money Round(int decimals = 2) => new(Currency, decimal.Round(Amount, decimals, MidpointRounding.ToEven));

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Cannot operate on Money of different currencies: {Currency} vs {other.Currency}.");
        }
    }

    public override bool Equals(object? obj) => base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();

    /// <summary>Compara por moeda e valor (igualdade estrutural de value object).</summary>
    public static bool operator ==(Money? left, Money? right) => Equals(left, right);

    public static bool operator !=(Money? left, Money? right) => !Equals(left, right);

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public static bool operator >(Money left, Money right)
    {
        left.EnsureSameCurrency(right);
        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        left.EnsureSameCurrency(right);
        return left.Amount < right.Amount;
    }

    public static bool operator >=(Money left, Money right)
    {
        left.EnsureSameCurrency(right);
        return left.Amount >= right.Amount;
    }

    public static bool operator <=(Money left, Money right)
    {
        left.EnsureSameCurrency(right);
        return left.Amount <= right.Amount;
    }

    public int CompareTo(Money? other)
    {
        if (other is null)
        {
            return 1;
        }

        EnsureSameCurrency(other);
        return Amount.CompareTo(other.Amount);
    }

    /// <summary>Retorna o total percentual (ex.: 0.72 = 72%) com base em um total.</summary>
    public static decimal Percentage(Money part, Money total)
    {
        part.EnsureSameCurrency(total);
        if (total.Amount == 0m)
        {
            return 0m;
        }

        return Math.Abs(part.Amount) / Math.Abs(total.Amount);
    }

    public string ToDisplayString() =>
        Amount.ToString("C", new CultureInfo("pt-BR"));

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Currency;
        yield return Amount;
    }

    public override string ToString() => $"{Amount} {Currency}";
}
