using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public class Account : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; }

    public Guid? LinkedBankAccountId { get; set; }

    public decimal? CreditLimit { get; set; }

    public decimal InitialBalance { get; set; }

    public decimal CurrentBalance { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public ICollection<Transfer> OutgoingTransfers { get; set; } = new List<Transfer>();

    public ICollection<Transfer> IncomingTransfers { get; set; } = new List<Transfer>();
}
