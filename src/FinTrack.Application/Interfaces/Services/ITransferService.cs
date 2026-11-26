using FinTrack.Application.Common;
using FinTrack.Application.DTOs.Transfers;

namespace FinTrack.Application.Interfaces.Services;

public interface ITransferService
{
    Task<Result<List<TransferResponse>>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<TransferResponse>> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<TransferResponse>> CreateAsync(
        Guid userId,
        CreateTransferRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);
}
