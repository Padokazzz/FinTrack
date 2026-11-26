using FinTrack.Domain.Entities;

namespace FinTrack.Application.Interfaces.Repositories;

public interface ITransferRepository
{
    Task<List<Transfer>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<(List<Transfer> Items, int TotalCount)> GetPageAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken);

    Task<Transfer?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(Transfer transfer, CancellationToken cancellationToken = default);

    void Delete(Transfer transfer);
}
