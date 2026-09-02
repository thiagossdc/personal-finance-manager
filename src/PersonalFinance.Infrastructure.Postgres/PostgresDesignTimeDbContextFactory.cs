using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PersonalFinance.Infrastructure.Persistence;

namespace PersonalFinance.Infrastructure.Postgres;

/// <summary>
/// Fábrica de contexto usada apenas em design-time (dotnet ef) para gerar as
/// migrations específicas do Postgres. A conexão vem de ConnectionStrings__Postgres
/// (env var) com fallback local padrão do docker-compose.
/// </summary>
public sealed class PostgresDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=personalfinance;Username=finance;Password=finance_dev";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npg =>
            {
                npg.MigrationsAssembly("PersonalFinance.Infrastructure.Postgres");
            })
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new AppDbContext(options, new PersonalFinance.Infrastructure.Persistence.VersionedEntityInterceptor());
    }
}
