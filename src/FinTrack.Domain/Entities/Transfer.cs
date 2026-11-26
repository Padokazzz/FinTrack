using FinTrack.Domain.Common;

namespace FinTrack.Domain.Entities;

public class Transfer : BaseEntity
{
    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public Guid FromAccountId { get; set; }

    public Account FromAccount { get; set; } = null!;

    public Guid ToAccountId { get; set; }

    public Account ToAccount { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;
}
