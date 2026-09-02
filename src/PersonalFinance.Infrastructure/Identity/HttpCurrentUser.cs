using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Services;

namespace PersonalFinance.Infrastructure.Identity;

/// <summary>
/// Implementa <see cref="ICurrentUser"/> a partir das claims do token JWT da requisição HTTP atual.
/// </summary>
public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid? UserId
    {
        get
        {
            var user = _accessor.HttpContext?.User;
            return user is null ? null : ServiceBase.GetUserIdFromClaims(user);
        }
    }

    public bool IsAuthenticated => _accessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
