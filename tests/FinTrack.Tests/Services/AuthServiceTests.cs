using FinTrack.Application.DTOs.Auth;
using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Application.Interfaces.Security;
using FinTrack.Application.Services;
using FinTrack.Domain.Entities;
using FluentAssertions;
using Moq;

namespace FinTrack.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenEmailAlreadyExists()
    {
        var request = new RegisterRequest
        {
            Name = "John Doe",
            Email = "john@example.com",
            Password = "123456"
        };

        _userRepositoryMock
            .Setup(repository => repository.EmailExistsAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.RegisterAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Email is already registered.");

        _userRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenEmailDoesNotExist()
    {
        var request = new RegisterRequest
        {
            Name = "John Doe",
            Email = "john@example.com",
            Password = "123456"
        };

        User? createdUser = null;

        _userRepositoryMock
            .Setup(repository => repository.EmailExistsAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(hasher => hasher.Hash(request.Password))
            .Returns("hashed-password");

        _jwtTokenGeneratorMock
            .Setup(generator => generator.GenerateToken(It.IsAny<User>()))
            .Returns("fake-jwt-token");

        _userRepositoryMock
            .Setup(repository => repository.AddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => createdUser = user)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _service.RegisterAsync(request);

        result.IsSuccess.Should().BeTrue();

        createdUser.Should().NotBeNull();
        createdUser!.Name.Should().Be(request.Name);
        createdUser.Email.Should().Be(request.Email);
        createdUser.PasswordHash.Should().Be("hashed-password");

        result.Value.Token.Should().Be("fake-jwt-token");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnFailure_WhenUserDoesNotExist()
    {
        var request = new LoginRequest
        {
            Email = "john@example.com",
            Password = "123456"
        };

        _userRepositoryMock
            .Setup(repository => repository.GetByEmailAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _service.LoginAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnFailure_WhenPasswordIsInvalid()
    {
        var request = new LoginRequest
        {
            Email = "john@example.com",
            Password = "wrong-password"
        };

        var user = new User
        {
            Name = "John Doe",
            Email = "john@example.com",
            PasswordHash = "hashed-password"
        };

        _userRepositoryMock
            .Setup(repository => repository.GetByEmailAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(hasher => hasher.Verify(request.Password, user.PasswordHash))
            .Returns(false);

        var result = await _service.LoginAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
    {
        var request = new LoginRequest
        {
            Email = "john@example.com",
            Password = "123456"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "John Doe",
            Email = "john@example.com",
            PasswordHash = "hashed-password"
        };

        _userRepositoryMock
            .Setup(repository => repository.GetByEmailAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(hasher => hasher.Verify(request.Password, user.PasswordHash))
            .Returns(true);

        _jwtTokenGeneratorMock
            .Setup(generator => generator.GenerateToken(user))
            .Returns("fake-jwt-token");

        var result = await _service.LoginAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(user.Id);
        result.Value.Name.Should().Be(user.Name);
        result.Value.Email.Should().Be(user.Email);
        result.Value.Token.Should().Be("fake-jwt-token");
    }
}