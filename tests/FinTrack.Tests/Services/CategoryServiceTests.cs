using FinTrack.Application.DTOs.Categories;
using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Application.Services;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FluentAssertions;
using Moq;

namespace FinTrack.Tests.Services;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _service = new CategoryService(
            _categoryRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnFailure_WhenCategoryAlreadyExists()
    {
        var userId = Guid.NewGuid();

        var request = new CreateCategoryRequest
        {
            Name = "Food",
            Type = TransactionType.Expense
        };

        _categoryRepositoryMock
            .Setup(repository => repository.ExistsByNameAndTypeAsync(
                userId,
                request.Name,
                request.Type,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.CreateAsync(userId, request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("A category with the same name and type already exists.");

        _categoryRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateCategory_WhenCategoryDoesNotExist()
    {
        var userId = Guid.NewGuid();

        var request = new CreateCategoryRequest
        {
            Name = "Food",
            Type = TransactionType.Expense
        };

        Category? createdCategory = null;

        _categoryRepositoryMock
            .Setup(repository => repository.ExistsByNameAndTypeAsync(
                userId,
                request.Name,
                request.Type,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _categoryRepositoryMock
            .Setup(repository => repository.AddAsync(
                It.IsAny<Category>(),
                It.IsAny<CancellationToken>()))
            .Callback<Category, CancellationToken>((category, _) => createdCategory = category)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _service.CreateAsync(userId, request);

        result.IsSuccess.Should().BeTrue();

        createdCategory.Should().NotBeNull();
        createdCategory!.Name.Should().Be(request.Name);
        createdCategory.Type.Should().Be(request.Type);
        createdCategory.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFailure_WhenCategoryDoesNotExist()
    {
        var categoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _categoryRepositoryMock
            .Setup(repository => repository.GetByIdAndUserIdAsync(
                categoryId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var result = await _service.GetByIdAsync(categoryId, userId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Category not found.");
    }
}