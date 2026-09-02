using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PersonalFinance.Api.Filters;
using PersonalFinance.Api.Middleware;
using PersonalFinance.Application;
using PersonalFinance.Application.Abstractions;
using PersonalFinance.Application.Common;
using PersonalFinance.Infrastructure;
using PersonalFinance.Infrastructure.Persistence;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// ---- Serilog ----
// CA1305: os overloads Console/File do Serilog não recebem IFormatProvider (falso positivo).
#pragma warning disable CA1305
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day);
});
#pragma warning restore CA1305

// ---- Services ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---- JWT Authentication ----
var jwtOptions = builder.Configuration.GetSection("Jwt");
var secretKey = jwtOptions["SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey is required.");
if (secretKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:SecretKey must be at least 32 characters long for HS256.");
}
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions["Issuer"],
        ValidAudience = jwtOptions["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ---- Rate Limiting ----
// Proteção contra brute-force nos endpoints de autenticação. Partição por IP.
// Em Development o limite é alto (não interfere em testes/debug); em produção o
// padrão é 10 solicitações/minuto por IP (configurável em RateLimiting:AuthPerMinute).
// O limiter é sempre registrado para que [EnableRateLimiting("auth")] seja válido
// em todos os ambientes.
var perIp = builder.Configuration.GetValue<int?>(
    "RateLimiting:AuthPerMinute")
    ?? (builder.Environment.IsDevelopment() ? 100_000 : 10);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = perIp,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst
            }));
});

// ---- CORS ----
// Restrito por padrão: sem wildcard de origem. Origens permitidas são lidas de
// "Cors:AllowedOrigins" (ex.: "https://app.example.com,https://admin.example.com").
// Um cliente nativo (MAUI) não depende de CORS; apenas clientes web o utilizam.
var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Api", policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        // Sem origens configuradas => nenhuma origem é liberada para cross-origin.
    });
});

// ---- Controllers & JSON ----
builder.Services.AddControllers(options =>
    {
        // Validação automática via FluentValidation (validadores registrados em AddApplication).
        options.Filters.Add<FluentValidationActionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// ---- Swagger ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---- Health Checks ----
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

// ---- Middleware Pipeline ----
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Api");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health");

// ---- Auto-migrate and seed in development ----
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await ((AppDbContext)db).Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db, passwordHasher);
}

app.Run();

public partial class Program { }
