using FinTrack.Domain.Enums;

namespace FinTrack.Application.DTOs.Categories;

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;

    public TransactionType Type { get; set; }
}