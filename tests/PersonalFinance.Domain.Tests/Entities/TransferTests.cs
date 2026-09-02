using FluentAssertions;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.ValueObjects;

namespace PersonalFinance.Domain.Tests.Entities;

public class TransferTests
{
    private static readonly Guid SourceId = Guid.NewGuid();
    private static readonly Guid TargetId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var result = Transfer.Create(Guid.NewGuid(), SourceId, TargetId, Money.From(500m), DateTime.UtcNow, "Transferência");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Amount.Should().Be(500m);
        result.Value.IsCanceled.Should().BeFalse();
    }

    [Fact]
    public void Create_SameSourceAndTarget_Fails()
    {
        var result = Transfer.Create(Guid.NewGuid(), SourceId, SourceId, Money.From(100m), DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithZeroAmount_Fails()
    {
        var result = Transfer.Create(Guid.NewGuid(), SourceId, TargetId, Money.Zero(), DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void GetNetAmountForAccount_Source_IsNegative()
    {
        var transfer = Transfer.Create(Guid.NewGuid(), SourceId, TargetId, Money.From(500m), DateTime.UtcNow).Value!;

        transfer.GetNetAmountForAccount(SourceId).Amount.Should().Be(-500m);
    }

    [Fact]
    public void GetNetAmountForAccount_Target_IsPositive()
    {
        var transfer = Transfer.Create(Guid.NewGuid(), SourceId, TargetId, Money.From(500m), DateTime.UtcNow).Value!;

        transfer.GetNetAmountForAccount(TargetId).Amount.Should().Be(500m);
    }

    [Fact]
    public void Cancel_SetsIsCanceled()
    {
        var transfer = Transfer.Create(Guid.NewGuid(), SourceId, TargetId, Money.From(100m), DateTime.UtcNow).Value!;

        transfer.Cancel();

        transfer.IsCanceled.Should().BeTrue();
    }

    [Fact]
    public void Transfer_NeverCreatesOrDestroysMoney()
    {
        // Invariante financeira: a soma do efeito líquido sobre origem e destino deve
        // ser sempre zero (a transferência apenas movimenta valor entre contas).
        var transfer = Transfer.Create(Guid.NewGuid(), SourceId, TargetId, Money.From(500.75m), DateTime.UtcNow).Value!;

        var sourceDelta = transfer.GetNetAmountForAccount(SourceId);
        var targetDelta = transfer.GetNetAmountForAccount(TargetId);

        (sourceDelta.Amount + targetDelta.Amount).Should().Be(0m);
    }

    [Fact]
    public void InstallmentSplit_AllPartsSumToTotal_EvenWithRounding()
    {
        // Reproduz a geração de parcelas (a última compensa o arredondamento do resto)
        // e valida que a soma das parcelas é exatamente o total — nunca criando nem
        // destruindo dinheiro por arredondamento.
        var total = 1000m;
        const int count = 3;
        var perInstallment = Decimal.Round(total / count, 2, MidpointRounding.AwayFromZero);

        var installments = new List<decimal>();
        for (var i = 1; i <= count; i++)
        {
            var amount = i == count ? total - (perInstallment * (count - 1)) : perInstallment;
            installments.Add(amount);
        }

        installments.Sum().Should().Be(total);
        installments[0].Should().Be(333.33m);
        installments[^1].Should().Be(333.34m, because: "a última parcela absorve o arredondamento");
    }
}
