using FinTrack.Application.DTOs.Auth;
using FinTrack.Application.Validators.Auth;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace FinTrack.Tests.Validators.Auth;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldHaveError_WhenNameIsEmpty()
    {
        var request = new RegisterRequest
        {
            Name = "",
            Email = "john@example.com",
            Password = "123456"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEmailIsInvalid()
    {
        var request = new RegisterRequest
        {
            Name = "John Doe",
            Email = "invalid-email",
            Password = "123456"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenPasswordIsTooShort()
    {
        var request = new RegisterRequest
        {
            Name = "John Doe",
            Email = "john@example.com",
            Password = "123"
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenRequestIsValid()
    {
        var request = new RegisterRequest
        {
            Name = "John Doe",
            Email = "john@example.com",
            Password = "123456"
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }
}