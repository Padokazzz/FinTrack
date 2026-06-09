using FinTrack.Domain.Enums;

namespace FinTrack.Application.DTOs.Categories;

public class CategoryResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public TransactionType Type { get; set; }
}