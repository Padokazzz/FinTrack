using FinTrack.Application.DTOs.Transfers;
using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Application.Services;
using FinTrack.Domain.Entities;
using FluentAssertions;
using Moq;

namespace FinTrack.Tests.Services;

public class TransferServiceTests
{
    private readonly Mock<ITransferRepository> _transferRepositoryMock = new();
    private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private readonly TransferService _service;

    public TransferServiceTests()
    {
        _service = new TransferService(
            _transferRepositoryMock.Object,
            _accountRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldMoveBalanceBetweenAccounts()
    {
        var userId = Guid.NewGuid();
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();

        var fromAccount = new Account
        {
            Id = fromAccountId,
            Name = "Checking",
            CurrentBalance = 1000,
            UserId = userId
        };

        var toAccount = new Account
        {
            Id = toAccountId,
            Name = "Investment",
            CurrentBalance = 250,
            UserId = userId
        };

        var request = new CreateTransferRequest
        {
            Description = "Transfer to investments",
            Amount = 300,
            Date = new DateTime(2026, 7, 1),
            FromAccountId = fromAccountId,
            ToAccountId = toAccountId
        };

        _accountRepositoryMock
            .Setup(repository => repository.GetByIdAndUserIdAsync(
                fromAccountId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromAccount);

        _accountRepositoryMock
            .Setup(repository => repository.GetByIdAndUserIdAsync(
                toAccountId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(toAccount);

        var result = await _service.CreateAsync(userId, request);

        result.IsSuccess.Should().BeTrue();
        fromAccount.CurrentBalance.Should().Be(700);
        toAccount.CurrentBalance.Should().Be(550);
        result.Value.FromAccountName.Should().Be("Checking");
        result.Value.ToAccountName.Should().Be("Investment");

        _transferRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Transfer>(transfer =>
                    transfer.Amount == request.Amount &&
                    transfer.FromAccountId == fromAccountId &&
                    transfer.ToAccountId == toAccountId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenAccountsAreTheSame()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var request = new CreateTransferRequest
        {
            Description = "Invalid transfer",
            Amount = 100,
            Date = new DateTime(2026, 7, 1),
            FromAccountId = accountId,
            ToAccountId = accountId
        };

        var result = await _service.CreateAsync(userId, request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Origin and destination accounts must be different.");
    }

    [Fact]
    public async Task DeleteAsync_ShouldReverseTransferBalance()
    {
        var userId = Guid.NewGuid();
        var transferId = Guid.NewGuid();
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();

        var transfer = new Transfer
        {
            Id = transferId,
            Description = "Transfer to investments",
            Amount = 300,
            Date = new DateTime(2026, 7, 1),
            FromAccountId = fromAccountId,
            ToAccountId = toAccountId,
            UserId = userId
        };

        var fromAccount = new Account
        {
            Id = fromAccountId,
            CurrentBalance = 700,
            UserId = userId
        };

        var toAccount = new Account
        {
            Id = toAccountId,
            CurrentBalance = 550,
            UserId = userId
        };

        _transferRepositoryMock
            .Setup(repository => repository.GetByIdAndUserIdAsync(
                transferId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transfer);

        _accountRepositoryMock
            .Setup(repository => repository.GetByIdAndUserIdAsync(
                fromAccountId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromAccount);

        _accountRepositoryMock
            .Setup(repository => repository.GetByIdAndUserIdAsync(
                toAccountId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(toAccount);

        var result = await _service.DeleteAsync(transferId, userId);

        result.IsSuccess.Should().BeTrue();
        fromAccount.CurrentBalance.Should().Be(1000);
        toAccount.CurrentBalance.Should().Be(250);

        _transferRepositoryMock.Verify(
            repository => repository.Delete(transfer),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
