using FinTrack.Application.DTOs.Categories;
using FluentValidation;

namespace FinTrack.Application.Validators.Categories;

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(80).WithMessage("Name must not exceed 80 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Category type is invalid.");
    }
}