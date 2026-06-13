using FinTrack.Application.DTOs.Accounts;
using FinTrack.Application.Validators.Accounts;
using FinTrack.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace FinTrack.Tests.Validators.Accounts;

public class CreateAccountRequestValidatorTests
{
    private readonly CreateAccountRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldHaveError_WhenNameIsEmpty()
    {
        var request = new CreateAccountRequest
        {
            Name = "",
            Type = AccountType.Checking,
            InitialBalance = 100
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenInitialBalanceIsNegative()
    {
        var request = new CreateAccountRequest
        {
            Name = "Main Account",
            Type = AccountType.Checking,
            InitialBalance = -1
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.InitialBalance);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTypeIsInvalid()
    {
        var request = new CreateAccountRequest
        {
            Name = "Main Account",
            Type = (AccountType)999,
            InitialBalance = 100
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenRequestIsValid()
    {
        var request = new CreateAccountRequest
        {
            Name = "Main Account",
            Type = AccountType.Checking,
            InitialBalance = 100
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }
}