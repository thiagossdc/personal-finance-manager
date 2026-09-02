namespace PersonalFinance.Domain.Common;

/// <summary>
/// Evento de domínio. Envelope base garantindo um identificador e o instante em que ocorreu.
/// </summary>
public abstract record DomainEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Base para entidades de domínio com identidade. Encapsula a coleção de eventos de domínio.
/// </summary>
public abstract class Entity
{
    private readonly List<DomainEvent> _domainEvents = new();

    public Guid Id { get; protected set; } = Guid.NewGuid();

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Inicializa a identidade a partir de um id gerado no dispositivo (offline-first).
    /// </summary>
    public virtual void AssignClientId(Guid id)
    {
        Id = id;
    }

    protected void AddDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// Base para value objects. Garante igualdade estrutural por valor.
/// </summary>
public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !Equals(left, right);
}
