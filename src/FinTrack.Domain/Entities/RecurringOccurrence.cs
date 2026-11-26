namespace FinTrack.Domain.Entities;

public class RecurringOccurrence
{
    public Guid RuleId { get; set; }
    public RecurringRule Rule { get; set; } = null!;
    public DateOnly Date { get; set; }
    public DateTime? PostedAt { get; set; }
}
