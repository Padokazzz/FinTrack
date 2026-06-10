using FinTrack.Domain.Entities;

namespace FinTrack.Application.Interfaces.Repositories;

public interface IAccountRepository
{
    Task<List<Account>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Account?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(Account account, CancellationToken cancellationToken = default);

    void Update(Account account);

    void Delete(Account account);
}