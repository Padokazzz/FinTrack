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
        return Filter(userId, month, year, type, categoryId)
            .Include(transaction => transaction.Account)
            .Include(transaction => transaction.Category)
            .OrderByDescending(transaction => transaction.Date)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Transaction> Filter(Guid userId, int? month, int? year, TransactionType? type, Guid? categoryId)
    {
        var query = _context.Transactions
            .AsNoTracking()
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
    
        return query;
    }

    public async Task<(List<Transaction> Items, int TotalCount)> GetPageAsync(
        Guid userId, int? month, int? year, TransactionType? type, Guid? categoryId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = Filter(userId, month, year, type, categoryId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(transaction => transaction.Account)
            .Include(transaction => transaction.Category)
            .OrderByDescending(transaction => transaction.Date)
            .ThenByDescending(transaction => transaction.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(decimal Income, decimal Expense)> GetMonthlyTotalsAsync(
        Guid userId, int month, int year, CancellationToken cancellationToken)
    {
        var totals = await Filter(userId, month, year, null, null)
            .GroupBy(transaction => transaction.UserId)
            .Select(group => new
            {
                Income = group.Sum(transaction => transaction.Type == TransactionType.Income ? transaction.Amount : 0m),
                Expense = group.Sum(transaction => transaction.Type == TransactionType.Expense ? transaction.Amount : 0m)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return (totals?.Income ?? 0m, totals?.Expense ?? 0m);
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