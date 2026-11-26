using FinTrack.Infrastructure.Recurrences;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinTrack.Api.Services;

public class RecurrenceWorker(IServiceScopeFactory scopes, ILogger<RecurrenceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        do
        {
            try
            {
                await ProcessDueAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Unable to process recurring transactions.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessDueAsync(CancellationToken cancellationToken)
    {
        using var scan = scopes.CreateScope();
        var due = await scan.ServiceProvider.GetRequiredService<RecurrenceService>().GetDueIdsAsync(cancellationToken);
        foreach (var id in due)
        {
            using var scope = scopes.CreateScope();
            try
            {
                var result = await scope.ServiceProvider.GetRequiredService<RecurrenceService>()
                    .ProcessNextAsync(id, cancellationToken);
                if (result.IsFailure)
                {
                    logger.LogWarning("Recurring rule {RuleId} could not be posted: {Reason}", id, result.Error);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogDebug("Concurrent update for recurring rule {RuleId}.", id);
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "PK_RecurringOccurrences" })
            {
                logger.LogDebug("Occurrence already materialized for rule {RuleId}.", id);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Unable to process recurring rule {RuleId}.", id);
            }
        }
    }
}
