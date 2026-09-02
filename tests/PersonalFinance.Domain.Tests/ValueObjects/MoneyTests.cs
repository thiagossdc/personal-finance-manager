using FluentAssertions;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithPositiveAmount_ReturnsSuccess()
    {
        var result = Money.Create(100.50m, "BRL");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(100.50m);
        result.Value.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Create_WithNegativeAmount_ReturnsFailure()
    {
        var result = Money.Create(-10m);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithIsNegative_FlipsSign()
    {
        var result = Money.Create(50m, isNegative: true);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(-50m);
    }

    [Fact]
    public void Add_SameCurrency_SumsCorrectly()
    {
        var a = Money.From(100m);
        var b = Money.From(50m);

        var sum = a.Add(b);

        sum.Amount.Should().Be(150m);
    }

    [Fact]
    public void Add_DifferentCurrency_Throws()
    {
        var a = Money.From(100m, "BRL");
        var b = Money.From(50m, "USD");

        var act = () => a.Add(b);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Subtract_SameCurrency_SubtractsCorrectly()
    {
        var a = Money.From(100m);
        var b = Money.From(30m);

        a.Subtract(b).Amount.Should().Be(70m);
    }

    [Fact]
    public void Round_UsesBankersRounding()
    {
        Money.From(1.005m).Round(2).Amount.Should().Be(1.00m);
        Money.From(1.015m).Round(2).Amount.Should().Be(1.02m);
    }

    [Fact]
    public void Percentage_CalculatesCorrectly()
    {
        var part = Money.From(720m);
        var total = Money.From(1000m);

        Money.Percentage(part, total).Should().Be(0.72m);
    }

    [Fact]
    public void Percentage_WithZeroTotal_ReturnsZero()
    {
        Money.Percentage(Money.From(100m), Money.Zero()).Should().Be(0m);
    }

    [Fact]
    public void Zero_ReturnsZeroAmount()
    {
        Money.Zero().Amount.Should().Be(0m);
        Money.Zero().IsZero.Should().BeTrue();
    }

    [Theory]
    [InlineData("brl", "BRL")]
    [InlineData(null, "BRL")]
    public void Create_NormalizesCurrency(string? input, string expected)
    {
        Money.Create(1m, input).Value!.Currency.Should().Be(expected);
    }

    [Fact]
    public void Comparison_OrdersByAmount()
    {
        var a = Money.From(100m);
        var b = Money.From(50m);

        (a > b).Should().BeTrue();
        (b < a).Should().BeTrue();
    }

    [Fact]
    public void Equality_SameValue_IsEqual()
    {
        var a = Money.Create(100m, "BRL").Value!;
        var b = Money.Create(100m, "BRL").Value!;

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentCurrency_IsNotEqual()
    {
        Money.Create(100m, "BRL").Value!.Should().NotBe(Money.Create(100m, "USD").Value!);
    }

    [Fact]
    public void ToDisplayString_FormatsAsBRL()
    {
        Money.Create(1234.56m, "BRL").Value!.ToDisplayString().Should().Contain("1.234,56");
    }
}
