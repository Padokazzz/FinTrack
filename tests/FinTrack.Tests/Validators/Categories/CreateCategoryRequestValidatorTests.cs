using FinTrack.Application.DTOs.Categories;
using FinTrack.Application.Validators.Categories;
using FinTrack.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace FinTrack.Tests.Validators.Categories;

public class CreateCategoryRequestValidatorTests
{
    private readonly CreateCategoryRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldHaveError_WhenNameIsEmpty()
    {
        var request = new CreateCategoryRequest
        {
            Name = "",
            Type = TransactionType.Expense
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenTypeIsInvalid()
    {
        var request = new CreateCategoryRequest
        {
            Name = "Food",
            Type = (TransactionType)999
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenRequestIsValid()
    {
        var request = new CreateCategoryRequest
        {
            Name = "Food",
            Type = TransactionType.Expense
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeTrue();
    }
}