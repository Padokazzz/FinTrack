using FinTrack.Domain.Enums;

namespace FinTrack.Application.DTOs.Recurrences;

public record CreateRecurrenceRequest(
    string Description, Guid AccountId, Guid CategoryId, TransactionType Type,
    decimal? Amount, DateOnly StartsOn, bool Automatic, RecurringPaymentMethod? PaymentMethod = null, DateOnly? EndsOn = null);

public record RecurrenceStatusRequest(bool Paused);
public record ConfirmOccurrenceRequest(decimal? Amount);

public record RecurrenceResponse(
    Guid Id, string Description, Guid AccountId, Guid CategoryId, TransactionType Type,
    decimal? Amount, DateOnly StartsOn, DateOnly NextDate, bool Automatic, bool Paused, RecurringPaymentMethod? PaymentMethod, DateOnly? EndsOn);

public record ScheduledOccurrence(
    Guid RuleId, string Description, Guid AccountId, TransactionType Type,
    decimal? Amount, DateOnly Date, bool Pending, bool Automatic, RecurringPaymentMethod? PaymentMethod);

public record ProjectedAccount(Guid Id, string Name, decimal CurrentBalance, decimal ProjectedBalance, bool HasUnknownAmounts);

public record RecurrenceAgenda(
    DateOnly Today, DateOnly Through, IReadOnlyList<ScheduledOccurrence> Items,
    IReadOnlyList<ProjectedAccount> Accounts);

public record RecordAmortizationRequest(Guid Id, DateOnly Date, decimal? Amount, int Installments);
public record AmortizationResponse(Guid Id, DateOnly Date, decimal? Amount, int Installments,
    decimal? RemainingBefore, decimal? RemainingAfter, decimal? ComparedPayment, decimal? Savings);

public record AmortizationComparison(decimal RemainingBefore, decimal RemainingAfter, decimal Payment);
