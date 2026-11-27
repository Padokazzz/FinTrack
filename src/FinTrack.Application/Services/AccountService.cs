using FinTrack.Application.Common;
using FinTrack.Application.DTOs.Accounts;
using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Application.Interfaces.Services;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;

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
        var error = await ValidateCardAsync(null, userId, request.Type, request.LinkedBankAccountId, request.CreditLimit, cancellationToken);
        if (error is not null)
            return Result<AccountResponse>.Failure(error);

        if (request.InitialUsedCredit is decimal used &&
            (request.Type != AccountType.CreditCard || request.InitialBalance != 0 ||
             used < 0 || used > 9_999_999_999_999_999.99m || decimal.Round(used, 2) != used))
            return Result<AccountResponse>.Failure("Enter a valid initial amount used on the card.");

        var openingBalance = -request.InitialUsedCredit ?? request.InitialBalance;
        var account = new Account
        {
            Name = request.Name,
            Type = request.Type,
            LinkedBankAccountId = request.LinkedBankAccountId,
            CreditLimit = request.CreditLimit,
            InitialBalance = openingBalance,
            CurrentBalance = openingBalance,
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

        var error = await ValidateCardAsync(id, userId, request.Type, request.LinkedBankAccountId, request.CreditLimit, cancellationToken);
        if (error is not null)
            return Result<AccountResponse>.Failure(error);

        if (IsBankAccount(account.Type) && !IsBankAccount(request.Type))
        {
            var accounts = await _accountRepository.GetAllByUserIdAsync(userId, cancellationToken);
            if (accounts.Any(card => card.LinkedBankAccountId == id))
                return Result<AccountResponse>.Failure("Unlink the cards before changing this account type.");
        }

        account.LinkedBankAccountId = request.LinkedBankAccountId;
        account.CreditLimit = request.CreditLimit;
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

    private async Task<string?> ValidateCardAsync(Guid? id, Guid userId, AccountType type,
        Guid? bankId, decimal? limit, CancellationToken cancellationToken)
    {
        if (type != AccountType.CreditCard && (bankId.HasValue || limit.HasValue))
            return "Only credit cards can have a linked bank account or a credit limit.";

        if (limit is decimal amount && (amount <= 0 || amount > 9_999_999_999_999_999.99m || decimal.Round(amount, 2) != amount))
            return "Enter a positive credit limit with up to two decimal places.";

        if (bankId is not Guid linkedId)
            return null;

        if (linkedId == id)
            return "Choose another account to link to the card.";

        var bank = await _accountRepository.GetByIdAndUserIdAsync(linkedId, userId, cancellationToken);
        return bank is not null && IsBankAccount(bank.Type) ? null : "Choose one of your checking or savings accounts.";
    }

    private static bool IsBankAccount(AccountType type) => type is AccountType.Checking or AccountType.Savings;

    private static AccountResponse MapToResponse(Account account)
    {
        return new AccountResponse
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type,
            LinkedBankAccountId = account.LinkedBankAccountId,
            CreditLimit = account.CreditLimit,
            InitialBalance = account.InitialBalance,
            CurrentBalance = account.CurrentBalance
        };
    }
}