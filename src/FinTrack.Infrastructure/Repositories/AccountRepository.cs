using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly FinTrackDbContext _context;

    public AccountRepository(FinTrackDbContext context)
    {
        _context = context;
    }

    public Task<List<Account>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Accounts
            .AsNoTracking()
            .Where(account => account.UserId == userId)
            .OrderBy(account => account.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Account?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Accounts
            .FirstOrDefaultAsync(account => account.Id == id && account.UserId == userId, cancellationToken);
    }

    public Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        return _context.Accounts.AddAsync(account, cancellationToken).AsTask();
    }

    public void Update(Account account)
    {
        _context.Accounts.Update(account);
    }

    public void Delete(Account account)
    {
        _context.Accounts.Remove(account);
    }
}