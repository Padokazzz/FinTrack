using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;

namespace FinTrack.Application.Interfaces.Repositories;

public interface ITransactionRepository
{
    Task<List<Transaction>> GetFilteredAsync(
        Guid userId,
        int? month,
        int? year,
        TransactionType? type,
        Guid? categoryId,
        CancellationToken cancellationToken = default);

    Task<(List<Transaction> Items, int TotalCount)> GetPageAsync(
        Guid userId, int? month, int? year, TransactionType? type, Guid? categoryId, int page, int pageSize, CancellationToken cancellationToken);

    Task<(decimal Income, decimal Expense)> GetMonthlyTotalsAsync(
        Guid userId, int month, int year, CancellationToken cancellationToken);

    Task<Transaction?> GetByIdAndUserIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    void Update(Transaction transaction);

    void Delete(Transaction transaction);
}