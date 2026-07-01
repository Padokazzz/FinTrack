using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Application.Services;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FluentAssertions;
using Moq;

namespace FinTrack.Tests.Services;

public class SummaryServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock = new();
    private readonly Mock<IAccountRepository> _accountRepositoryMock = new();

    private readonly SummaryService _service;

    public SummaryServiceTests()
    {
        _service = new SummaryService(
            _transactionRepositoryMock.Object,
            _accountRepositoryMock.Object);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_ShouldCalculateTotals()
    {
        var userId = Guid.NewGuid();
        var month = 6;
        var year = 2026;

        var transactions = new List<Transaction>
        {
            new()
            {
                Amount = 5000,
                Type = TransactionType.Income,
                Date = new DateTime(2026, 6, 5)
            },
            new()
            {
                Amount = 1200,
                Type = TransactionType.Expense,
                Date = new DateTime(2026, 6, 10)
            },
            new()
            {
                Amount = 300,
                Type = TransactionType.Expense,
                Date = new DateTime(2026, 6, 15)
            }
        };

        _transactionRepositoryMock
            .Setup(repository => repository.GetFilteredAsync(
                userId,
                month,
                year,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var result = await _service.GetMonthlySummaryAsync(userId, month, year);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalIncome.Should().Be(5000);
        result.Value.TotalExpense.Should().Be(1500);
        result.Value.FinalBalance.Should().Be(3500);
        result.Value.Month.Should().Be(month);
        result.Value.Year.Should().Be(year);
    }

    [Fact]
    public async Task GetOverallBalanceAsync_ShouldCalculateTotalBalance()
    {
        var userId = Guid.NewGuid();

        var accounts = new List<Account>
        {
            new()
            {
                CurrentBalance = 1500,
                UserId = userId
            },
            new()
            {
                CurrentBalance = 250,
                UserId = userId
            },
            new()
            {
                CurrentBalance = -100,
                UserId = userId
            }
        };

        _accountRepositoryMock
            .Setup(repository => repository.GetAllByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        var result = await _service.GetOverallBalanceAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalBalance.Should().Be(1650);
        result.Value.AccountsCount.Should().Be(3);
    }
}
