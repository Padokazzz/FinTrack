using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Repositories;

public class TransferRepository : ITransferRepository
{
    private readonly FinTrackDbContext _context;

    public TransferRepository(FinTrackDbContext context)
    {
        _context = context;
    }

    public Task<List<Transfer>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _context.Transfers
            .AsNoTracking()
            .Include(transfer => transfer.FromAccount)
            .Include(transfer => transfer.ToAccount)
            .Where(transfer => transfer.UserId == userId)
            .OrderByDescending(transfer => transfer.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Transfer> Items, int TotalCount)> GetPageAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Transfers.AsNoTracking().Where(transfer => transfer.UserId == userId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(transfer => transfer.FromAccount)
            .Include(transfer => transfer.ToAccount)
            .OrderByDescending(transfer => transfer.Date)
            .ThenByDescending(transfer => transfer.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<Transfer?> GetByIdAndUserIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _context.Transfers
            .Include(transfer => transfer.FromAccount)
            .Include(transfer => transfer.ToAccount)
            .FirstOrDefaultAsync(
                transfer => transfer.Id == id && transfer.UserId == userId,
                cancellationToken);
    }

    public Task AddAsync(Transfer transfer, CancellationToken cancellationToken = default)
    {
        return _context.Transfers.AddAsync(transfer, cancellationToken).AsTask();
    }

    public void Delete(Transfer transfer)
    {
        _context.Transfers.Remove(transfer);
    }
}
