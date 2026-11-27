using FinTrack.Domain.Enums;

namespace FinTrack.Application.DTOs.Accounts;

public class AccountResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; }

    public Guid? LinkedBankAccountId { get; set; }

    public decimal? CreditLimit { get; set; }

    public decimal InitialBalance { get; set; }

    public decimal CurrentBalance { get; set; }

    public decimal? UsedCredit => Type == AccountType.CreditCard ? Math.Max(0, -CurrentBalance) : null;

    public decimal? AvailableCredit => CreditLimit is decimal limit && UsedCredit is decimal used
        ? Math.Max(0, limit - used)
        : null;
}