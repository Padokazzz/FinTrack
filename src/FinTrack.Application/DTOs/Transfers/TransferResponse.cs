namespace FinTrack.Application.DTOs.Transfers;

public class TransferResponse
{
    public Guid Id { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public Guid FromAccountId { get; set; }

    public string FromAccountName { get; set; } = string.Empty;

    public Guid ToAccountId { get; set; }

    public string ToAccountName { get; set; } = string.Empty;
}
