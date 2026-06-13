using FinTrack.Application.DTOs.Accounts;
using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Application.Services;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FluentAssertions;
using Moq;

namespace FinTrack.Tests.Services;

public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private readonly AccountService _service;

    public AccountServiceTests()
    {
        _service = new AccountService(
            _accountRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateAccountWithCurrentBalanceEqualToInitialBalance()
    {
        var userId = Guid.NewGuid();

        var request = new CreateAccountRequest
        {
            Name = "Main Account",
            Type = AccountType.Checking,
            InitialBalance = 1000
        };

        Account? createdAccount = null;

        _accountRepositoryMock
            .Setup(repository => repository.AddAsync(
                It.IsAny<Account>(),
                It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((account, _) => createdAccount = account)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _service.CreateAsync(userId, request);

        result.IsSuccess.Should().BeTrue();

        createdAccount.Should().NotBeNull();
        createdAccount!.Name.Should().Be(request.Name);
        createdAccount.Type.Should().Be(request.Type);
        createdAccount.InitialBalance.Should().Be(request.InitialBalance);
        createdAccount.CurrentBalance.Should().Be(request.InitialBalance);
        createdAccount.UserId.Should().Be(userId);

        _unitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFailure_WhenAccountDoesNotExist()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _accountRepositoryMock
            .Setup(repository => repository.GetByIdAndUserIdAsync(
                accountId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var result = await _service.GetByIdAsync(accountId, userId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Account not found.");
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFailure_WhenAccountDoesNotExist()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _accountRepositoryMock
            .Setup(repository => repository.GetByIdAndUserIdAsync(
                accountId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var result = await _service.DeleteAsync(accountId, userId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Account not found.");

        _unitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}