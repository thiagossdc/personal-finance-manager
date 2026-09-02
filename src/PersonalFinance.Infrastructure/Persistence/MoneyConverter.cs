using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Infrastructure.Persistence;

/// <summary>
/// Converte <see cref="Money"/> para <see cref="decimal"/> (amount) na persistência.
/// A aplicação roda em pt-BR/BRL nesta versão; a moeda permanece no objeto Money na fronteira
/// da aplicação, permitindo evolução futura para multi-moeda (ver docs/database.md).
/// </summary>
public sealed class MoneyConverter : ValueConverter<Money, decimal>
{
    public MoneyConverter()
        : base(
            money => money.Amount,
            amount => Money.From(amount))
    {
    }
}
