using FinTrack.Application.Common;
using FinTrack.Application.DTOs.Accounts;

namespace FinTrack.Application.Interfaces.Services;

public interface IAccountService
{
    Task<Result<List<AccountResponse>>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<AccountResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<Result<AccountResponse>> CreateAsync(Guid userId, CreateAccountRequest request, CancellationToken cancellationToken = default);

    Task<Result<AccountResponse>> UpdateAsync(Guid id, Guid userId, UpdateAccountRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}