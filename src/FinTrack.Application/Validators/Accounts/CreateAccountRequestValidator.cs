using FinTrack.Application.DTOs.Accounts;
using FinTrack.Domain.Enums;
using FluentValidation;

namespace FinTrack.Application.Validators.Accounts;

public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Account type is invalid.");

        RuleFor(x => x.InitialBalance)
            .GreaterThanOrEqualTo(0).WithMessage("Initial balance must be zero or greater.")
            .PrecisionScale(18, 2, true).WithMessage("Initial balance must have up to 16 integer digits and 2 decimal places.");
    }
}