using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Services;

namespace PersonalFinance.Application;

/// <summary>Registra os serviços e validadores da camada de aplicação.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddSingleton<IDateTime, SystemDateTime>();

        services.AddScoped<BalanceCalculator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<ICreditCardService, CreditCardService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IGoalService, GoalService>();
        services.AddScoped<IRecurringTransactionService, RecurringTransactionService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISyncService, SyncService>();
        return services;
    }
}
