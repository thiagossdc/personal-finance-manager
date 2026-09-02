using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Domain.Entities;

/// <summary>
/// Categoria hierárquica (ex.: Alimentação → Restaurantes). Exclusão segura exige migração
/// das dependências; por isso o domínio expõe apenas desativação.
/// </summary>
public class Category : Entity, IVersioned
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Icon { get; private set; }
    public Guid? ParentId { get; private set; }
    public Category? Parent { get; private set; }
    public TransactionType Type { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    private readonly List<Category> _children = new();
    public IReadOnlyCollection<Category> Children => _children.AsReadOnly();

    private Category() { } // EF Core

    public static Result<Category> Create(Guid userId, string name, TransactionType type, string? icon = null, Guid? parentId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("Category name is required.");
        }

        var now = DateTime.UtcNow;
        var category = new Category
        {
            UserId = userId,
            Name = name.Trim(),
            Type = type,
            Icon = icon,
            ParentId = parentId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        return Result<Category>.Success(category);
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("Category name is required.");
        }

        Name = name.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public void Update(string? name, string? icon)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name.Trim();
        }

        Icon = icon;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetParent(Guid? parentId)
    {
        ParentId = parentId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void BumpVersion() => Version++;
}

/// <summary>Etiqueta simples usada para organizar transações.</summary>
public class Tag : Entity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private Tag() { }

    public static Result<Tag> Create(Guid userId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("Tag name is required.");
        }

        return Result<Tag>.Success(new Tag
        {
            UserId = userId,
            Name = name.Trim().ToLowerInvariant()
        });
    }
}
