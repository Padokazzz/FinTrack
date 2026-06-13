using FinTrack.Application.Common;
using FinTrack.Application.DTOs.Transactions;

namespace FinTrack.Application.Interfaces.Services;

public interface ITransactionService
{
    Task<Result<List<TransactionResponse>>> GetFilteredAsync(
        Guid userId,
        TransactionFilterRequest filter,
        CancellationToken cancellationToken = default);

    Task<Result<TransactionResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<Result<TransactionResponse>> CreateAsync(Guid userId, CreateTransactionRequest request, CancellationToken cancellationToken = default);

    Task<Result<TransactionResponse>> UpdateAsync(Guid id, Guid userId, UpdateTransactionRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}