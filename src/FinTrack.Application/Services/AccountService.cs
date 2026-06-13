using FinTrack.Application.Common;
using FinTrack.Application.DTOs.Accounts;
using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Application.Interfaces.Services;
using FinTrack.Domain.Entities;

namespace FinTrack.Application.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AccountService(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<AccountResponse>>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var accounts = await _accountRepository.GetAllByUserIdAsync(userId, cancellationToken);

        var response = accounts.Select(MapToResponse).ToList();

        return Result<List<AccountResponse>>.Success(response);
    }

    public async Task<Result<AccountResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAndUserIdAsync(id, userId, cancellationToken);

        if (account is null)
        {
            return Result<AccountResponse>.Failure("Account not found.");
        }

        return Result<AccountResponse>.Success(MapToResponse(account));
    }

    public async Task<Result<AccountResponse>> CreateAsync(Guid userId, CreateAccountRequest request, CancellationToken cancellationToken = default)
    {
        var account = new Account
        {
            Name = request.Name,
            Type = request.Type,
            InitialBalance = request.InitialBalance,
            CurrentBalance = request.InitialBalance,
            UserId = userId
        };

        await _accountRepository.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AccountResponse>.Success(MapToResponse(account));
    }

    public async Task<Result<AccountResponse>> UpdateAsync(Guid id, Guid userId, UpdateAccountRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAndUserIdAsync(id, userId, cancellationToken);

        if (account is null)
        {
            return Result<AccountResponse>.Failure("Account not found.");
        }

        account.Name = request.Name;
        account.Type = request.Type;
        account.UpdatedAt = DateTime.UtcNow;

        _accountRepository.Update(account);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AccountResponse>.Success(MapToResponse(account));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAndUserIdAsync(id, userId, cancellationToken);

        if (account is null)
        {
            return Result.Failure("Account not found.");
        }

        _accountRepository.Delete(account);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static AccountResponse MapToResponse(Account account)
    {
        return new AccountResponse
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type,
            InitialBalance = account.InitialBalance,
            CurrentBalance = account.CurrentBalance
        };
    }
}