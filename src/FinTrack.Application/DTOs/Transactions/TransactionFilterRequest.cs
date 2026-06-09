using FinTrack.Domain.Enums;

namespace FinTrack.Application.DTOs.Transactions;

public class TransactionFilterRequest
{
    public int? Month { get; set; }

    public int? Year { get; set; }

    public TransactionType? Type { get; set; }

    public Guid? CategoryId { get; set; }
}