using FinTrack.Application.Common;
using FinTrack.Application.DTOs.Recurrences;

namespace FinTrack.Application.Interfaces.Services;

public interface IRecurrenceService
{
    Task<List<RecurrenceResponse>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<Result<RecurrenceResponse>> CreateAsync(Guid userId, CreateRecurrenceRequest request, CancellationToken cancellationToken);
    Task<bool> SetPausedAsync(Guid userId, Guid id, bool paused, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<RecurrenceAgenda> GetAgendaAsync(Guid userId, CancellationToken cancellationToken);
    Task<Result> ConfirmAsync(Guid userId, Guid ruleId, DateOnly date, decimal? amount, CancellationToken cancellationToken);
}
