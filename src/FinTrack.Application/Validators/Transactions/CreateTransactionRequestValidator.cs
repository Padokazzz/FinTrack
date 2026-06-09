using FinTrack.Application.DTOs.Transactions;
using FluentValidation;

namespace FinTrack.Application.Validators.Transactions;

public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(150).WithMessage("Description must not exceed 150 characters.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Transaction type is invalid.");

        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account is required.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");
    }
}