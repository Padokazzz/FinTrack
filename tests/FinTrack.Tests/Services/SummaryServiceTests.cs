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

    private readonly SummaryService _service;

    public SummaryServiceTests()
    {
        _service = new SummaryService(_transactionRepositoryMock.Object);
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
}