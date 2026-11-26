using FinTrack.Application.Common;
using FinTrack.Application.DTOs.Summaries;
using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Application.Interfaces.Services;

namespace FinTrack.Application.Services;

public class SummaryService : ISummaryService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;

    public SummaryService(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
    }

    public async Task<Result<MonthlySummaryResponse>> GetMonthlySummaryAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        var (totalIncome, totalExpense) = await _transactionRepository.GetMonthlyTotalsAsync(
            userId, month, year, cancellationToken);

        var response = new MonthlySummaryResponse
        {
            Month = month,
            Year = year,
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            FinalBalance = totalIncome - totalExpense
        };

        return Result<MonthlySummaryResponse>.Success(response);
    }

    public async Task<Result<OverallBalanceResponse>> GetOverallBalanceAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var (totalBalance, accountsCount) = await _accountRepository.GetOverallBalanceAsync(
            userId,
            cancellationToken);

        var response = new OverallBalanceResponse
        {
            TotalBalance = totalBalance,
            AccountsCount = accountsCount
        };

        return Result<OverallBalanceResponse>.Success(response);
    }
}
