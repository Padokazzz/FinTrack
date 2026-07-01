using FinTrack.Application.Common;
using FinTrack.Application.DTOs.Summaries;

namespace FinTrack.Application.Interfaces.Services;

public interface ISummaryService
{
    Task<Result<MonthlySummaryResponse>> GetMonthlySummaryAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default);

    Task<Result<OverallBalanceResponse>> GetOverallBalanceAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
