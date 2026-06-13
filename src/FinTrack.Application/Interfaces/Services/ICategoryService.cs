using FinTrack.Application.Common;
using FinTrack.Application.DTOs.Categories;

namespace FinTrack.Application.Interfaces.Services;

public interface ICategoryService
{
    Task<Result<List<CategoryResponse>>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<CategoryResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<Result<CategoryResponse>> CreateAsync(Guid userId, CreateCategoryRequest request, CancellationToken cancellationToken = default);

    Task<Result<CategoryResponse>> UpdateAsync(Guid id, Guid userId, UpdateCategoryRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}