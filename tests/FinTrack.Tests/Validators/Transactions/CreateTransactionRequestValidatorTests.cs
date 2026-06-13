using FinTrack.Application.DTOs.Transactions;
using FinTrack.Application.Validators.Transactions;
using FinTrack.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace FinTrack.Tests.Validators.Transactions;

public class CreateTransactionRequestValidatorTests
{
    private readonly CreateTransactionRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldHaveError_WhenDescriptionIsEmpty()
    {
        var request = CreateValidRequest();
        request.Description = "";

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenAmountIsZero()
    {
        var request = CreateValidRequest();
        request.Amount = 0;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenAmountIsNegative()
    {
        var request = CreateValidRequest();
        request.Amount = -10;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTypeIsInvalid()
    {
        var request = CreateValidRequest();
        request.Type = (TransactionType)999;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenAccountIdIsEmpty()
    {
        var request = CreateValidRequest();
        request.AccountId = Guid.Empty;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.AccountId);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCategoryIdIsEmpty()
    {
        var request = CreateValidRequest();
        request.CategoryId = Guid.Empty;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenRequestIsValid()
    {
        var request = CreateValidRequest();

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }

    private static CreateTransactionRequest CreateValidRequest()
    {
        return new CreateTransactionRequest
        {
            Description = "Groceries",
            Amount = 100,
            Date = DateTime.UtcNow,
            Type = TransactionType.Expense,
            AccountId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid()
        };
    }
}