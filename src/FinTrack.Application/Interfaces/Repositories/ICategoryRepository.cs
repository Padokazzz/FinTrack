using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;

namespace FinTrack.Application.Interfaces.Repositories;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAndTypeAsync(
        Guid userId,
        string name,
        TransactionType type,
        CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    void Update(Category category);

    void Delete(Category category);
}