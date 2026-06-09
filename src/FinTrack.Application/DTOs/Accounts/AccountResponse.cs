using FinTrack.Domain.Enums;

namespace FinTrack.Application.DTOs.Accounts;

public class AccountResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; }

    public decimal InitialBalance { get; set; }

    public decimal CurrentBalance { get; set; }
}