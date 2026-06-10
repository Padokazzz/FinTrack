using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly FinTrackDbContext _context;

    public CategoryRepository(FinTrackDbContext context)
    {
        _context = context;
    }

    public Task<List<Category>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Categories
            .AsNoTracking()
            .Where(category => category.UserId == userId)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Category?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Categories
            .FirstOrDefaultAsync(category => category.Id == id && category.UserId == userId, cancellationToken);
    }

    public Task<bool> ExistsByNameAndTypeAsync(
        Guid userId,
        string name,
        TransactionType type,
        CancellationToken cancellationToken = default)
    {
        return _context.Categories
            .AsNoTracking()
            .AnyAsync(category =>
                category.UserId == userId &&
                category.Name == name &&
                category.Type == type,
                cancellationToken);
    }

    public Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        return _context.Categories.AddAsync(category, cancellationToken).AsTask();
    }

    public void Update(Category category)
    {
        _context.Categories.Update(category);
    }

    public void Delete(Category category)
    {
        _context.Categories.Remove(category);
    }
}