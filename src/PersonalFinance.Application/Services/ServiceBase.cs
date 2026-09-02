using System.Security.Claims;
using PersonalFinance.Application.Common;
using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Application.Services;

/// <summary>
/// Base das aplicações de serviços. Provê acesso ao contexto de persistência, ao usuário atual
/// e ao relógio, além de helpers de escopo (evita IDOR garantindo filtro por usuário).
/// </summary>
public abstract class ServiceBase
{
    protected ServiceBase(IAppDbContext db, ICurrentUser currentUser, IDateTime time)
    {
        Db = db;
        CurrentUser = currentUser;
        Time = time;
    }

    protected IAppDbContext Db { get; }
    protected ICurrentUser CurrentUser { get; }
    protected IDateTime Time { get; }

    /// <summary>Id do usuário atual ou erro de autenticação.</summary>
    protected Guid RequireUserId()
    {
        return CurrentUser.UserId ?? throw new UnauthorizedAccessException("Authentication required.");
    }

    public static Guid? GetUserIdFromClaims(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst("uid");
        return Guid.TryParse(claim?.Value, out var id) ? id : null;
    }
}
