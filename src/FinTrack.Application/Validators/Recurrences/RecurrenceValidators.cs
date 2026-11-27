using FinTrack.Application.DTOs.Recurrences;
using FluentValidation;
using FinTrack.Domain.Enums;

namespace FinTrack.Application.Validators.Recurrences;

public class CreateRecurrenceRequestValidator : AbstractValidator<CreateRecurrenceRequest>
{
    public CreateRecurrenceRequestValidator()
    {
        RuleFor(request => request.Description).NotEmpty().MaximumLength(150);
        RuleFor(request => request.AccountId).NotEmpty();
        RuleFor(request => request.CategoryId).NotEmpty();
        RuleFor(request => request.Type).IsInEnum();
        RuleFor(request => request.StartsOn)
            .InclusiveBetween(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31));
        RuleFor(request => request.EndsOn)
            .Must((request, endsOn) => !endsOn.HasValue || (endsOn.Value >= request.StartsOn && endsOn.Value <= new DateOnly(2100, 12, 31)))
            .WithMessage("The end date must be on or after the first payment and no later than 2100.");
        RuleFor(request => request.PaymentMethod)
            .IsInEnum()
            .Must((request, method) => method == null || request.Type == TransactionType.Expense)
            .WithMessage("Payment method is only available for expenses.");
        RuleFor(request => request.Amount).NotNull().When(request => request.Automatic);
        RuleFor(request => request.Amount).GreaterThan(0m).PrecisionScale(18, 2, true)
            .When(request => request.Amount.HasValue);
    }
}

public class ConfirmOccurrenceRequestValidator : AbstractValidator<ConfirmOccurrenceRequest>
{
    public ConfirmOccurrenceRequestValidator()
    {
        RuleFor(request => request.Amount).GreaterThan(0m).PrecisionScale(18, 2, true)
            .When(request => request.Amount.HasValue);
    }
}

public class RecordAmortizationRequestValidator : AbstractValidator<RecordAmortizationRequest>
{
    public RecordAmortizationRequestValidator()
    {
        RuleFor(request => request.Id).NotEmpty();
        RuleFor(request => request.Date).GreaterThanOrEqualTo(new DateOnly(2000, 1, 1));
        RuleFor(request => request.Amount).GreaterThan(0m).PrecisionScale(18, 2, true)
            .When(request => request.Amount.HasValue);
        RuleFor(request => request.Installments).InclusiveBetween(0, 1212);
        RuleFor(request => request).Must(request => request.Amount.HasValue || request.Installments > 0)
            .WithMessage("Enter an amount or a number of installments.");
    }
}

public class AmortizationComparisonValidator : AbstractValidator<AmortizationComparison>
{
    public AmortizationComparisonValidator()
    {
        RuleFor(request => request.RemainingBefore).GreaterThanOrEqualTo(0m).PrecisionScale(18, 2, true);
        RuleFor(request => request.RemainingAfter).GreaterThanOrEqualTo(0m).PrecisionScale(18, 2, true);
        RuleFor(request => request.Payment).GreaterThan(0m).PrecisionScale(18, 2, true);
    }
}
