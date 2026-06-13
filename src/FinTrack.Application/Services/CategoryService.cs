using FinTrack.Application.Common;
using FinTrack.Application.DTOs.Categories;
using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Application.Interfaces.Services;
using FinTrack.Domain.Entities;

namespace FinTrack.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<CategoryResponse>>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllByUserIdAsync(userId, cancellationToken);

        var response = categories.Select(MapToResponse).ToList();

        return Result<List<CategoryResponse>>.Success(response);
    }

    public async Task<Result<CategoryResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAndUserIdAsync(id, userId, cancellationToken);

        if (category is null)
        {
            return Result<CategoryResponse>.Failure("Category not found.");
        }

        return Result<CategoryResponse>.Success(MapToResponse(category));
    }

    public async Task<Result<CategoryResponse>> CreateAsync(Guid userId, CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await _categoryRepository.ExistsByNameAndTypeAsync(
            userId,
            request.Name,
            request.Type,
            cancellationToken);
    
        if (exists)
        {
            return Result<CategoryResponse>.Failure("A category with the same name and type already exists.");
        }

        var category = new Category
        {
            Name = request.Name,
            Type = request.Type,
            UserId = userId
        };

        await _categoryRepository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CategoryResponse>.Success(MapToResponse(category));
    }

    public async Task<Result<CategoryResponse>> UpdateAsync(Guid id, Guid userId, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAndUserIdAsync(id, userId, cancellationToken);

        if (category is null)
        {
            return Result<CategoryResponse>.Failure("Category not found.");
        }

        var exists = await _categoryRepository.ExistsByNameAndTypeAsync(
            userId,
            request.Name,
            request.Type,
            cancellationToken);

        if (exists && (category.Name != request.Name || category.Type != request.Type))
        {
            return Result<CategoryResponse>.Failure("A category with the same name and type already exists.");
        }

        category.Name = request.Name;
        category.Type = request.Type;
        category.UpdatedAt = DateTime.UtcNow;

        _categoryRepository.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CategoryResponse>.Success(MapToResponse(category));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAndUserIdAsync(id, userId, cancellationToken);

        if (category is null)
        {
            return Result.Failure("Category not found.");
        }

        _categoryRepository.Delete(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static CategoryResponse MapToResponse(Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type
        };
    }
}