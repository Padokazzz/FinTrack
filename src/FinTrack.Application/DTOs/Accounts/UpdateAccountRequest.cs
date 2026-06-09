using FinTrack.Domain.Enums;

namespace FinTrack.Application.DTOs.Accounts;

public class UpdateAccountRequest
{
    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; }
}