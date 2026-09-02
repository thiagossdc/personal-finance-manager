using FluentAssertions;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Domain.Tests.Entities;

public sealed class CreditCardTests
{
    [Fact]
    public void Create_ShouldInitializeWithLimit()
    {
        var result = CreditCard.Create(Guid.NewGuid(), "Cartão Nubank", Money.Create(5000m).Value!, 10, 17);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Cartão Nubank");
        result.Value.CreditLimit.Amount.Should().Be(5000m);
        result.Value.ClosingDay.Should().Be(10);
        result.Value.DueDay.Should().Be(17);
    }

    [Fact]
    public void Create_WithInvalidClosingDay_ShouldFail()
    {
        var result = CreditCard.Create(Guid.NewGuid(), "Cartão", Money.Create(5000m).Value!, 32, 17);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AvailableLimit_AfterPurchase_ShouldDecrease()
    {
        var card = CreditCard.Create(Guid.NewGuid(), "Cartão", Money.Create(5000m).Value!, 10, 17).Value!;
        var purchase = CreditCardTransaction.Create(Guid.NewGuid(), card.Id, "Compra", Money.Create(1500m).Value!, DateTime.UtcNow).Value!;

        card.AddPurchase(purchase);

        card.AvailableLimit().Amount.Should().Be(3500m);
        card.CurrentOpenAmount().Amount.Should().Be(1500m);
    }

    [Fact]
    public void UtilizationRatio_ShouldCalculateCorrectly()
    {
        var card = CreditCard.Create(Guid.NewGuid(), "Cartão", Money.Create(10000m).Value!, 10, 17).Value!;
        var purchase = CreditCardTransaction.Create(Guid.NewGuid(), card.Id, "Compra", Money.Create(2500m).Value!, DateTime.UtcNow).Value!;

        card.AddPurchase(purchase);

        card.UtilizationRatio().Should().Be(0.25m);
    }

    [Fact]
    public void InstallmentPurchase_ShouldGenerateInstallments()
    {
        var purchase = CreditCardTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), "Notebook", Money.Create(3600m).Value!, DateTime.UtcNow, 12).Value!;

        var perInstallment = 300m;
        var due = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1);
        for (var i = 1; i <= 12; i++)
        {
            var amount = i == 12 ? 3600m - (perInstallment * 11) : perInstallment;
            var installment = Installment.Create(Guid.NewGuid(), purchase.Id, i, 12, Money.Create(amount).Value!, due).Value!;
            purchase.AddInstallment(installment);
            due = due.AddMonths(1);
        }

        purchase.Installments.Count.Should().Be(12);
        purchase.Installments.Sum(i => i.Amount.Amount).Should().Be(3600m);
    }
}
