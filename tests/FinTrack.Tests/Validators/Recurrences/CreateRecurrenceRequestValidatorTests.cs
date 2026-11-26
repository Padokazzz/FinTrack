using FinTrack.Application.DTOs.Recurrences;
using FinTrack.Application.Validators.Recurrences;
using FinTrack.Domain.Enums;
using FluentValidation.TestHelper;

namespace FinTrack.Tests.Validators.Recurrences;

public class CreateRecurrenceRequestValidatorTests
{
    private readonly CreateRecurrenceRequestValidator _validator = new();

    [Fact]
    public void AutomaticRecurrenceRequiresAnAmount()
    {
        var request = CreateRequest() with { Automatic = true };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(rule => rule.Amount);
    }

    [Fact]
    public void ManualRecurrenceCanLeaveTheAmountOpen()
    {
        _validator.TestValidate(CreateRequest()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EstimatedAmountMustUseValidCurrencyPrecision()
    {
        var request = CreateRequest() with { Amount = 10.001m };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(rule => rule.Amount);
    }

    private static CreateRecurrenceRequest CreateRequest() =>
        new("Internet", Guid.NewGuid(), Guid.NewGuid(), TransactionType.Expense, null,
            new DateOnly(2026, 11, 30), false);
}
