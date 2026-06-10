using FinTrack.Application.Interfaces.Repositories;

namespace FinTrack.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly FinTrackDbContext _context;

    public UnitOfWork(FinTrackDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}