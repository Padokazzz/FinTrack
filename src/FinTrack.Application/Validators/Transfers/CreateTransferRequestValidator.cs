using FinTrack.Application.DTOs.Transfers;
using FluentValidation;

namespace FinTrack.Application.Validators.Transfers;

public class CreateTransferRequestValidator : AbstractValidator<CreateTransferRequest>
{
    public CreateTransferRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(150).WithMessage("Description must not exceed 150 characters.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.")
            .PrecisionScale(18, 2, true).WithMessage("Amount must have up to 16 integer digits and 2 decimal places.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required.");

        RuleFor(x => x.FromAccountId)
            .NotEmpty().WithMessage("Origin account is required.");

        RuleFor(x => x.ToAccountId)
            .NotEmpty().WithMessage("Destination account is required.")
            .NotEqual(x => x.FromAccountId).WithMessage("Origin and destination accounts must be different.");
    }
}
