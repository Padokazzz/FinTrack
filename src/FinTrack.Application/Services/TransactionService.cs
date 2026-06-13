using FinTrack.Application.Common;
using FinTrack.Application.DTOs.Transactions;
using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Application.Interfaces.Services;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;

namespace FinTrack.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TransactionService(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<TransactionResponse>>> GetFilteredAsync(
        Guid userId,
        TransactionFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var transactions = await _transactionRepository.GetFilteredAsync(
            userId,
            filter.Month,
            filter.Year,
            filter.Type,
            filter.CategoryId,
            cancellationToken);
    
        var response = transactions.Select(MapToResponse).ToList();

        return Result<List<TransactionResponse>>.Success(response);
    }

    public async Task<Result<TransactionResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAndUserIdAsync(id, userId, cancellationToken);

        if (transaction is null)
        {
            return Result<TransactionResponse>.Failure("Transaction not found.");
        }

        return Result<TransactionResponse>.Success(MapToResponse(transaction));
    }

    public async Task<Result<TransactionResponse>> CreateAsync(Guid userId, CreateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAndUserIdAsync(request.AccountId, userId, cancellationToken);

        if (account is null)
        {
            return Result<TransactionResponse>.Failure("Account not found.");
        }

        var category = await _categoryRepository.GetByIdAndUserIdAsync(request.CategoryId, userId, cancellationToken);

        if (category is null)
        {
            return Result<TransactionResponse>.Failure("Category not found.");
        }

        if (category.Type != request.Type)
        {
            return Result<TransactionResponse>.Failure("Category type must match transaction type.");
        }

        var transaction = new Transaction
        {
            Description = request.Description,
            Amount = request.Amount,
            Date = request.Date,
            Type = request.Type,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            UserId = userId,
            Account = account,
            Category = category
        };

        ApplyTransactionToAccount(account, transaction);

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        _accountRepository.Update(account);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TransactionResponse>.Success(MapToResponse(transaction));
    }

    public async Task<Result<TransactionResponse>> UpdateAsync(Guid id, Guid userId, UpdateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAndUserIdAsync(id, userId, cancellationToken);

        if (transaction is null)
        {
            return Result<TransactionResponse>.Failure("Transaction not found.");
        }

        var oldAccount = await _accountRepository.GetByIdAndUserIdAsync(transaction.AccountId, userId, cancellationToken);

        if (oldAccount is null)
        {
            return Result<TransactionResponse>.Failure("Original account not found.");
        }

        var newAccount = await _accountRepository.GetByIdAndUserIdAsync(request.AccountId, userId, cancellationToken);

        if (newAccount is null)
        {
            return Result<TransactionResponse>.Failure("Account not found.");
        }

        var category = await _categoryRepository.GetByIdAndUserIdAsync(request.CategoryId, userId, cancellationToken);

        if (category is null)
        {
            return Result<TransactionResponse>.Failure("Category not found.");
        }

        if (category.Type != request.Type)
        {
            return Result<TransactionResponse>.Failure("Category type must match transaction type.");
        }

        ReverseTransactionFromAccount(oldAccount, transaction);

        transaction.Description = request.Description;
        transaction.Amount = request.Amount;
        transaction.Date = request.Date;
        transaction.Type = request.Type;
        transaction.AccountId = request.AccountId;
        transaction.CategoryId = request.CategoryId;
        transaction.Category = category;
        transaction.Account = newAccount;
        transaction.UpdatedAt = DateTime.UtcNow;

        ApplyTransactionToAccount(newAccount, transaction);

        _transactionRepository.Update(transaction);
        _accountRepository.Update(oldAccount);

        if (oldAccount.Id != newAccount.Id)
        {
            _accountRepository.Update(newAccount);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TransactionResponse>.Success(MapToResponse(transaction));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAndUserIdAsync(id, userId, cancellationToken);

        if (transaction is null)
        {
            return Result.Failure("Transaction not found.");
        }

        var account = await _accountRepository.GetByIdAndUserIdAsync(transaction.AccountId, userId, cancellationToken);

        if (account is null)
        {
            return Result.Failure("Account not found.");
        }

        ReverseTransactionFromAccount(account, transaction);

        _transactionRepository.Delete(transaction);
        _accountRepository.Update(account);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static void ApplyTransactionToAccount(Account account, Transaction transaction)
    {
        if (transaction.Type == TransactionType.Income)
        {
            account.CurrentBalance += transaction.Amount;
            return;
        }

        account.CurrentBalance -= transaction.Amount;
    }

    private static void ReverseTransactionFromAccount(Account account, Transaction transaction)
    {
        if (transaction.Type == TransactionType.Income)
        {
            account.CurrentBalance -= transaction.Amount;
            return;
        }

        account.CurrentBalance += transaction.Amount;
    }

    private static TransactionResponse MapToResponse(Transaction transaction)
    {
        return new TransactionResponse
        {
            Id = transaction.Id,
            Description = transaction.Description,
            Amount = transaction.Amount,
            Date = transaction.Date,
            Type = transaction.Type,
            AccountId = transaction.AccountId,
            AccountName = transaction.Account?.Name ?? string.Empty,
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category?.Name ?? string.Empty
        };
    }
}