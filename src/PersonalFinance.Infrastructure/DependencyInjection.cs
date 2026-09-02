using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinance.Application;
using PersonalFinance.Application.Abstractions;
using PersonalFinance.Application.Common;
using PersonalFinance.Infrastructure.Identity;
using PersonalFinance.Infrastructure.Persistence;

namespace PersonalFinance.Infrastructure;

/// <summary>Registra a camada de infraestrutura (persistência, identidade, cache).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var defaultConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=finance.sqlite";
        var postgresConnectionString = configuration.GetConnectionString("Postgres");
        var usePostgres = configuration.GetValue<bool?>("Database:UsePostgres") == true
            || !string.IsNullOrEmpty(postgresConnectionString);
        // Correção: ao usar Postgres, a connection string do Postgres (não a do SQLite
        // de DefaultConnection) é passada ao Npgsql. Antes, o provider Postgres era
        // selecionado, mas recebia "Data Source=finance.sqlite" — falhando no boot.
        var connectionString = usePostgres && !string.IsNullOrEmpty(postgresConnectionString)
            ? postgresConnectionString
            : defaultConnectionString;

        services.AddDbContext<AppDbContext>(options =>
        {
            if (usePostgres)
            {
                options.UseNpgsql(connectionString, npg =>
                {
                    // Migrations específicas do Postgres (bool → boolean, timestamps
                    // timestamptz etc.), pois as migrations do assembly principal foram
                    // geradas sob o provider SQLite e não são portáveis.
                    npg.MigrationsAssembly("PersonalFinance.Infrastructure.Postgres");
                    npg.EnableRetryOnFailure(3);
                })
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            }
            else
            {
                options.UseSqlite(defaultConnectionString, sql =>
                {
                    sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                });
            }
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddSingleton<VersionedEntityInterceptor>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddOptions<Identity.JwtOptions>().Bind(configuration.GetSection(Identity.JwtOptions.SectionName));

        var redis = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redis))
        {
            services.AddStackExchangeRedisCache(o => o.Configuration = redis);
        }

        return services;
    }
}
