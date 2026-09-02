namespace PersonalFinance.Application.Common;

/// <summary>Fornece o usuário autenticado no contexto atual da requisição.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }
}

/// <summary>Abstração do relógio, facilitando testes determinísticos.</summary>
public interface IDateTime
{
    DateTime UtcNow { get; }
}

public sealed class SystemDateTime : IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public sealed class AnonymousCurrentUser : ICurrentUser
{
    public Guid? UserId => null;
    public bool IsAuthenticated => false;
}
