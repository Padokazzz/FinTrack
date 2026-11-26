using FinTrack.Application.Common;
using FinTrack.Application.DTOs.Transfers;
using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Application.Interfaces.Services;
using FinTrack.Domain.Entities;

namespace FinTrack.Application.Services;

public class TransferService : ITransferService
{
    private readonly ITransferRepository _transferRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TransferService(
        ITransferRepository transferRepository,
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork)
    {
        _transferRepository = transferRepository;
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<TransferResponse>>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var transfers = await _transferRepository.GetAllByUserIdAsync(
            userId,
            cancellationToken);

        return Result<List<TransferResponse>>.Success(
            transfers.Select(MapToResponse).ToList());
    }

    public async Task<PaginatedResponse<TransferResponse>> GetPageAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _transferRepository.GetPageAsync(
            userId, page, pageSize, cancellationToken);

        return new PaginatedResponse<TransferResponse>(
            items.Select(MapToResponse).ToList(), page, pageSize, totalCount);
    }

    public async Task<Result<TransferResponse>> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetByIdAndUserIdAsync(
            id,
            userId,
            cancellationToken);

        if (transfer is null)
        {
            return Result<TransferResponse>.Failure("Transfer not found.");
        }

        return Result<TransferResponse>.Success(MapToResponse(transfer));
    }

    public async Task<Result<TransferResponse>> CreateAsync(
        Guid userId,
        CreateTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FromAccountId == request.ToAccountId)
        {
            return Result<TransferResponse>.Failure("Origin and destination accounts must be different.");
        }

        var fromAccount = await _accountRepository.GetByIdAndUserIdAsync(
            request.FromAccountId,
            userId,
            cancellationToken);

        if (fromAccount is null)
        {
            return Result<TransferResponse>.Failure("Origin account not found.");
        }

        var toAccount = await _accountRepository.GetByIdAndUserIdAsync(
            request.ToAccountId,
            userId,
            cancellationToken);

        if (toAccount is null)
        {
            return Result<TransferResponse>.Failure("Destination account not found.");
        }

        var transfer = new Transfer
        {
            Description = request.Description,
            Amount = request.Amount,
            Date = request.Date,
            FromAccountId = request.FromAccountId,
            FromAccount = fromAccount,
            ToAccountId = request.ToAccountId,
            ToAccount = toAccount,
            UserId = userId
        };

        ApplyTransfer(fromAccount, toAccount, transfer.Amount);

        await _transferRepository.AddAsync(transfer, cancellationToken);
        _accountRepository.Update(fromAccount);
        _accountRepository.Update(toAccount);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TransferResponse>.Success(MapToResponse(transfer));
    }

    public async Task<Result> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetByIdAndUserIdAsync(
            id,
            userId,
            cancellationToken);

        if (transfer is null)
        {
            return Result.Failure("Transfer not found.");
        }

        var fromAccount = await _accountRepository.GetByIdAndUserIdAsync(
            transfer.FromAccountId,
            userId,
            cancellationToken);

        if (fromAccount is null)
        {
            return Result.Failure("Origin account not found.");
        }

        var toAccount = await _accountRepository.GetByIdAndUserIdAsync(
            transfer.ToAccountId,
            userId,
            cancellationToken);

        if (toAccount is null)
        {
            return Result.Failure("Destination account not found.");
        }

        ReverseTransfer(fromAccount, toAccount, transfer.Amount);

        _transferRepository.Delete(transfer);
        _accountRepository.Update(fromAccount);
        _accountRepository.Update(toAccount);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static void ApplyTransfer(Account fromAccount, Account toAccount, decimal amount)
    {
        fromAccount.CurrentBalance -= amount;
        toAccount.CurrentBalance += amount;
    }

    private static void ReverseTransfer(Account fromAccount, Account toAccount, decimal amount)
    {
        fromAccount.CurrentBalance += amount;
        toAccount.CurrentBalance -= amount;
    }

    private static TransferResponse MapToResponse(Transfer transfer)
    {
        return new TransferResponse
        {
            Id = transfer.Id,
            Description = transfer.Description,
            Amount = transfer.Amount,
            Date = transfer.Date,
            FromAccountId = transfer.FromAccountId,
            FromAccountName = transfer.FromAccount?.Name ?? string.Empty,
            ToAccountId = transfer.ToAccountId,
            ToAccountName = transfer.ToAccount?.Name ?? string.Empty
        };
    }
}
