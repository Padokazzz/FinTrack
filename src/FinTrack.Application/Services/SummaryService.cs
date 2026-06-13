using FinTrack.Application.Common;
using FinTrack.Application.DTOs.Summaries;
using FinTrack.Application.DTOs.Transactions;
using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Application.Interfaces.Services;
using FinTrack.Domain.Enums;

namespace FinTrack.Application.Services;

public class SummaryService : ISummaryService
{
    private readonly ITransactionRepository _transactionRepository;

    public SummaryService(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<MonthlySummaryResponse>> GetMonthlySummaryAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        var transactions = await _transactionRepository.GetFilteredAsync(
            userId,
            month,
            year,
            null,
            null,
            cancellationToken);

        var totalIncome = transactions
            .Where(transaction => transaction.Type == TransactionType.Income)
            .Sum(transaction => transaction.Amount);

        var totalExpense = transactions
            .Where(transaction => transaction.Type == TransactionType.Expense)
            .Sum(transaction => transaction.Amount);

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
}