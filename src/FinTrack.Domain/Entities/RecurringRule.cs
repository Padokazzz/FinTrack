using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public class RecurringRule : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid AccountId { get; set; }
    public Guid CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public decimal? Amount { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly NextDate { get; set; }
    public bool Automatic { get; set; }
    public bool Paused { get; set; }
}
