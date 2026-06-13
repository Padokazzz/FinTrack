using FinTrack.Application.DTOs.Transactions;
using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Application.Services;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FluentAssertions;
using Moq;

namespace FinTrack.Tests.Services;

public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock = new();
    private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _service = new TransactionService(
            _transactionRepositoryMock.Object,
            _accountRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    //Income

    [Fact]
    public async Task CreateAsync_ShouldIncreaseAccountBalance_WhenTransactionIsIncome()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var account = new Account
        {
            Id = accountId,
            UserId = userId,
            Name = "Main Account",
            CurrentBalance = 1000
        };

        var category = new Category
        {
            Id = categoryId,
            UserId = userId,
            Name = "Salary",
            Type = TransactionType.Income
        };

        var request = new CreateTransactionRequest
        {
            Description = "Salary",
            Amount = 5000,
            Date = new DateTime(2026, 6, 5),
            Type = TransactionType.Income,
            AccountId = accountId,
            CategoryId = categoryId
        };

        _accountRepositoryMock
            .Setup(repository => repository.GetByIdAndUserIdAsync(
                accountId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _categoryRepositoryMock
            .Setup(repository => repository.GetByIdAndUserIdAsync(
                categoryId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _transactionRepositoryMock
            .Setup(repository => repository.AddAsync(
                It.IsAny<Transaction>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _service.CreateAsync(userId, request);

        result.IsSuccess.Should().BeTrue();
        account.CurrentBalance.Should().Be(6000);

        _accountRepositoryMock.Verify(
            repository => repository.Update(account),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    //Expense

    [Fact]
    public async Task CreateAsync_ShouldDecreaseAccountBalance_WhenTransactionIsExpense()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var account = new Account
        {
            Id = accountId,
            UserId = userId,
            Name = "Main Account",
            CurrentBalance = 1000
        };

        var category = new Category
        {
            Id = categoryId,
            UserId = userId,
            Name = "Food",
            Type = TransactionType.Expense
        };

        var request = new CreateTransactionRequest
        {
            Description = "Groceries",
            Amount = 250,
            Date = new DateTime(2026, 6, 5),
            Type = TransactionType.Expense,
            AccountId = accountId,
            CategoryId = categoryId
        };

        _accountRepositoryMock
            .Setup(repository => repository.GetByIdAndUserIdAsync(
                accountId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _categoryRepositoryMock
            .Setup(repository => repository.GetByIdAndUserIdAsync(
                categoryId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _transactionRepositoryMock
            .Setup(repository => repository.AddAsync(
                It.IsAny<Transaction>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _service.CreateAsync(userId, request);

        result.IsSuccess.Should().BeTrue();
        account.CurrentBalance.Should().Be(750);
    }

    //Account fail

    [Fact]
    public async Task CreateAsync_ShouldReturnFailure_WhenAccountDoesNotExist()
    {
        var userId = Guid.NewGuid();

        var request = new CreateTransactionRequest
        {
            Description = "Groceries",
            Amount = 250,
            Date = new DateTime(2026, 6, 5),
            Type = TransactionType.Expense,
            AccountId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid()
        };

        _accountRepositoryMock
            .Setup(repository => repository.GetByIdAndUserIdAsync(
                request.AccountId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var result = await _service.CreateAsync(userId, request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Account not found.");

        _transactionRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    //Category fail
    [Fact]
    public async Task CreateAsync_ShouldReturnFailure_WhenCategoryDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var account = new Account
        {
            Id = accountId,
            UserId = userId,
            CurrentBalance = 1000
        };

        var request = new CreateTransactionRequest
        {
            Description = "Groceries",
            Amount = 250,
            Date = new DateTime(2026, 6, 5),
            Type = TransactionType.Expense,
            AccountId = accountId,
            CategoryId = Guid.NewGuid()
        };

        _accountRepositoryMock
            .Setup(repository => repository.GetByIdAndUserIdAsync(
                accountId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _categoryRepositoryMock
            .Setup(repository => repository.GetByIdAndUserIdAsync(
                request.CategoryId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var result = await _service.CreateAsync(userId, request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Category not found.");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnFailure_WhenCategoryTypeDoesNotMatchTransactionType()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var account = new Account
        {
            Id = accountId,
            UserId = userId,
            CurrentBalance = 1000
        };

        var category = new Category
        {
            Id = categoryId,
            UserId = userId,
            Name = "Salary",
            Type = TransactionType.Income
        };

        var request = new CreateTransactionRequest
        {
            Description = "Groceries",
            Amount = 250,
            Date = new DateTime(2026, 6, 5),
            Type = TransactionType.Expense,
            AccountId = accountId,
            CategoryId = categoryId
        };

        _accountRepositoryMock
            .Setup(repository => repository.GetByIdAndUserIdAsync(
                accountId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _categoryRepositoryMock
            .Setup(repository => repository.GetByIdAndUserIdAsync(
                categoryId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var result = await _service.CreateAsync(userId, request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Category type must match transaction type.");

        account.CurrentBalance.Should().Be(1000);
    }
}