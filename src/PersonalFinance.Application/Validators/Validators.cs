using FluentValidation;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Application.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.InitialBalance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).Must(c => string.IsNullOrWhiteSpace(c) || c.Length == 3)
            .WithMessage("Currency must be a valid 3-letter ISO code.");
    }
}

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Type).IsInEnum();
    }
}

public sealed class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TransactionDate).NotEmpty();
    }
}

public sealed class CreateTransferRequestValidator : AbstractValidator<CreateTransferRequest>
{
    public CreateTransferRequestValidator()
    {
        RuleFor(x => x.SourceAccountId).NotEmpty();
        RuleFor(x => x.TargetAccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.TransferDate).NotEmpty();
        RuleFor(x => x).Must(x => x.SourceAccountId != x.TargetAccountId)
            .WithMessage("Source and target account must differ.")
            .WithName("transfer");
    }
}

public sealed class CreateCreditCardRequestValidator : AbstractValidator<CreateCreditCardRequest>
{
    public CreateCreditCardRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.CreditLimit).GreaterThan(0);
        RuleFor(x => x.ClosingDay).InclusiveBetween(1, 31);
        RuleFor(x => x.DueDay).InclusiveBetween(1, 31);
    }
}

public sealed class CreateCreditCardPurchaseRequestValidator : AbstractValidator<CreateCreditCardPurchaseRequest>
{
    public CreateCreditCardPurchaseRequestValidator()
    {
        RuleFor(x => x.CreditCardId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.InstallmentCount).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PurchaseDate).NotEmpty();
    }
}

public sealed class CreateBudgetRequestValidator : AbstractValidator<CreateBudgetRequest>
{
    public CreateBudgetRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Limit).GreaterThan(0);
        RuleFor(x => x.Period).IsInEnum();
        RuleFor(x => x.AlertThreshold).InclusiveBetween(0, 1)
            .When(x => x.AlertThreshold != default);
    }
}

public sealed class CreateGoalRequestValidator : AbstractValidator<CreateGoalRequest>
{
    public CreateGoalRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.TargetAmount).GreaterThan(0);
    }
}

public sealed class ContributeGoalRequestValidator : AbstractValidator<ContributeGoalRequest>
{
    public ContributeGoalRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public sealed class CreateRecurringTransactionRequestValidator : AbstractValidator<CreateRecurringTransactionRequest>
{
    public CreateRecurringTransactionRequestValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Frequency).IsInEnum();
        RuleFor(x => x.Interval).GreaterThanOrEqualTo(1);
        RuleFor(x => x.StartDate).NotEmpty();
    }
}
