using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public class RecurringOccurrence
{
    public Guid RuleId { get; set; }
    public RecurringRule Rule { get; set; } = null!;
    public DateOnly Date { get; set; }
    public Guid? OriginalAccountId { get; set; }
    public Guid? OriginalCategoryId { get; set; }
    public string? OriginalDescription { get; set; }
    public TransactionType? OriginalType { get; set; }
    public RecurringPaymentMethod? OriginalPaymentMethod { get; set; }
    public decimal? OriginalAmount { get; set; }
    public DateTime? PostedAt { get; set; }
}
