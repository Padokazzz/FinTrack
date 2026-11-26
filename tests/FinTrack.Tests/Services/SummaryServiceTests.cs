using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Application.Services;
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

        _transactionRepositoryMock
            .Setup(repository => repository.GetMonthlyTotalsAsync(userId, month, year, It.IsAny<CancellationToken>()))
            .ReturnsAsync((5000m, 1500m));

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

        _accountRepositoryMock
            .Setup(repository => repository.GetOverallBalanceAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((1650m, 3));

        var result = await _service.GetOverallBalanceAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalBalance.Should().Be(1650);
        result.Value.AccountsCount.Should().Be(3);
    }
}
