using FinTrack.Domain.Enums;

namespace FinTrack.Application.DTOs.Recurrences;

public record CreateRecurrenceRequest(
    string Description, Guid AccountId, Guid CategoryId, TransactionType Type,
    decimal? Amount, DateOnly StartsOn, bool Automatic);

public record RecurrenceStatusRequest(bool Paused);
public record ConfirmOccurrenceRequest(decimal? Amount);

public record RecurrenceResponse(
    Guid Id, string Description, Guid AccountId, Guid CategoryId, TransactionType Type,
    decimal? Amount, DateOnly StartsOn, DateOnly NextDate, bool Automatic, bool Paused);

public record ScheduledOccurrence(
    Guid RuleId, string Description, Guid AccountId, TransactionType Type,
    decimal? Amount, DateOnly Date, bool Pending, bool Automatic);

public record ProjectedAccount(Guid Id, string Name, decimal CurrentBalance, decimal ProjectedBalance, bool HasUnknownAmounts);

public record RecurrenceAgenda(
    DateOnly Today, DateOnly Through, IReadOnlyList<ScheduledOccurrence> Items,
    IReadOnlyList<ProjectedAccount> Accounts);
