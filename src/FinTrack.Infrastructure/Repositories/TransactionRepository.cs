using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly FinTrackDbContext _context;

    public TransactionRepository(FinTrackDbContext context)
    {
        _context = context;
    }

    public Task<List<Transaction>> GetFilteredAsync(
        Guid userId,
        int? month,
        int? year,
        TransactionType? type,
        Guid? categoryId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.Account)
            .Include(transaction => transaction.Category)
            .Where(transaction => transaction.UserId == userId);
    
        if (month.HasValue)
        {
            query = query.Where(transaction => transaction.Date.Month == month.Value);
        }
    
        if (year.HasValue)
        {
            query = query.Where(transaction => transaction.Date.Year == year.Value);
        }
    
        if (type.HasValue)
        {
            query = query.Where(transaction => transaction.Type == type.Value);
        }
    
        if (categoryId.HasValue)
        {
            query = query.Where(transaction => transaction.CategoryId == categoryId.Value);
        }
    
        return query
            .OrderByDescending(transaction => transaction.Date)
            .ToListAsync(cancellationToken);
    }

    public Task<Transaction?> GetByIdAndUserIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _context.Transactions
            .Include(transaction => transaction.Account)
            .Include(transaction => transaction.Category)
            .FirstOrDefaultAsync(transaction => transaction.Id == id && transaction.UserId == userId, cancellationToken);
    }

    public Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        return _context.Transactions.AddAsync(transaction, cancellationToken).AsTask();
    }

    public void Update(Transaction transaction)
    {
        _context.Transactions.Update(transaction);
    }

    public void Delete(Transaction transaction)
    {
        _context.Transactions.Remove(transaction);
    }
}