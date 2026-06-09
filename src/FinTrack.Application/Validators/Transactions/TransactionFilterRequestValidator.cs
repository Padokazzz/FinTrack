using FinTrack.Application.DTOs.Transactions;
using FluentValidation;

namespace FinTrack.Application.Validators.Transactions;

public class TransactionFilterRequestValidator : AbstractValidator<TransactionFilterRequest>
{
    public TransactionFilterRequestValidator()
    {
        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .When(x => x.Month.HasValue)
            .WithMessage("Month must be between 1 and 12.");

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100)
            .When(x => x.Year.HasValue)
            .WithMessage("Year must be between 2000 and 2100.");

        RuleFor(x => x.Type)
            .IsInEnum()
            .When(x => x.Type.HasValue)
            .WithMessage("Transaction type is invalid.");

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .When(x => x.CategoryId.HasValue)
            .WithMessage("Category is invalid.");
    }
}