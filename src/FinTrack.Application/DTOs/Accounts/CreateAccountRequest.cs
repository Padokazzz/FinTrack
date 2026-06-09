using FinTrack.Domain.Enums;

namespace FinTrack.Application.DTOs.Accounts;

public class CreateAccountRequest
{
    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; }

    public decimal InitialBalance { get; set; }
}