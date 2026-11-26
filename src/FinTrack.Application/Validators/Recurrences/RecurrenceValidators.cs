using FinTrack.Application.DTOs.Recurrences;
using FluentValidation;

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
