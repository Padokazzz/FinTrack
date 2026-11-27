namespace FinTrack.Domain.Entities;

public class RecurrenceAmortization
{
    public Guid Id { get; set; }
    public Guid RuleId { get; set; }
    public RecurringRule Rule { get; set; } = null!;
    public DateOnly Date { get; set; }
    public decimal? Amount { get; set; }
    public int Installments { get; set; }
    public decimal? RemainingBefore { get; set; }
    public decimal? RemainingAfter { get; set; }
    public decimal? ComparedPayment { get; set; }
    public decimal? Savings { get; set; }
}
