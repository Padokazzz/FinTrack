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
