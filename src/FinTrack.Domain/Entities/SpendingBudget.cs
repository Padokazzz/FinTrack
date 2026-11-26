namespace FinTrack.Domain.Entities;

public class SpendingBudget
{
    public Guid UserId { get; set; }

    public string Payload { get; set; } = string.Empty;
}
