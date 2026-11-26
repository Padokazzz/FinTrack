namespace FinTrack.Application.DTOs.Transfers;

public class CreateTransferRequest
{
    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public Guid FromAccountId { get; set; }

    public Guid ToAccountId { get; set; }
}
