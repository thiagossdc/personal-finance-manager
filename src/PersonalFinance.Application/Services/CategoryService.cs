using Microsoft.EntityFrameworkCore;
using PersonalFinance.Application.Common;
using PersonalFinance.Application.Dtos;
using PersonalFinance.Application.Services.Mapping;
using PersonalFinance.Domain.Common;
using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Application.Services;

public sealed class CategoryService : ServiceBase, ICategoryService
{
    public CategoryService(IAppDbContext db, ICurrentUser currentUser, IDateTime time)
        : base(db, currentUser, time)
    {
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var categories = await Db.Categories
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
        return Result<IReadOnlyList<CategoryDto>>.Success(categories.Select(c => c.ToDto()).ToList());
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        if (request.ParentId.HasValue)
        {
            var parentOk = await Db.Categories.AnyAsync(c => c.Id == request.ParentId && c.UserId == userId, ct);
            if (!parentOk)
            {
                return Error.NotFound("Parent category was not found.");
            }
        }

        var createResult = Category.Create(userId, request.Name, request.Type, request.Icon, request.ParentId);
        if (createResult.IsFailure)
        {
            return createResult.Error!;
        }

        Db.Categories.Add(createResult.Value!);
        await Db.SaveChangesAsync(ct);
        return Result<CategoryDto>.Success(createResult.Value!.ToDto());
    }

    public async Task<Result<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var category = await Db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);
        if (category is null)
        {
            return Error.NotFound("Category was not found.");
        }

        category.Update(request.Name, request.Icon);
        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
            {
                category.Activate();
            }
            else
            {
                category.Deactivate();
            }
        }

        await Db.SaveChangesAsync(ct);
        return Result<CategoryDto>.Success(category.ToDto());
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var userId = RequireUserId();
        var category = await Db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);
        if (category is null)
        {
            return Error.NotFound("Category was not found.");
        }

        var hasDependencies = await Db.Transactions.AnyAsync(t => t.CategoryId == id, ct)
            || await Db.Categories.AnyAsync(c => c.ParentId == id && c.IsActive, ct);
        if (hasDependencies)
        {
            // Exclusão segura: desativa, preservando histórico e vínculos.
            category.Deactivate();
        }
        else
        {
            Db.Categories.Remove(category);
        }

        await Db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
