using FinTrack.Domain.Common;

namespace FinTrack.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public Guid? PasswordVersion { get; set; }
    public Guid? PasswordResetId { get; set; }
    public string? PasswordResetHash { get; set; }
    public DateTime? PasswordResetSentAt { get; set; }
    public int PasswordResetAttempts { get; set; }

    public ICollection<Account> Accounts { get; set; } = new List<Account>();

    public ICollection<Category> Categories { get; set; } = new List<Category>();

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public ICollection<Transfer> Transfers { get; set; } = new List<Transfer>();
}
