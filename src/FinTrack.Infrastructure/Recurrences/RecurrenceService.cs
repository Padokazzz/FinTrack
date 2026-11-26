using System.Data;
using FinTrack.Application.Common;
using FinTrack.Application.DTOs.Recurrences;
using FinTrack.Application.DTOs.Transactions;
using FinTrack.Application.Interfaces.Services;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Recurrences;

public class RecurrenceService(
    FinTrackDbContext context,
    ITransactionService transactions,
    TimeProvider clock,
    TimeZoneInfo timeZone) : IRecurrenceService
{
    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), timeZone).DateTime);

    public Task<List<RecurrenceResponse>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        context.RecurringRules.AsNoTracking()
            .Where(rule => rule.UserId == userId)
            .OrderBy(rule => rule.Paused).ThenBy(rule => rule.NextDate).ThenBy(rule => rule.Id)
            .Select(rule => new RecurrenceResponse(
                rule.Id, rule.Description, rule.AccountId, rule.CategoryId, rule.Type, rule.Amount,
                rule.StartsOn, rule.NextDate, rule.Automatic, rule.Paused))
            .ToListAsync(cancellationToken);

    public async Task<Result<RecurrenceResponse>> CreateAsync(
        Guid userId, CreateRecurrenceRequest request, CancellationToken cancellationToken)
    {
        if (!await context.Accounts.AnyAsync(account => account.Id == request.AccountId && account.UserId == userId, cancellationToken))
        {
            return Result<RecurrenceResponse>.Failure("Account not found.");
        }

        if (!await context.Categories.AnyAsync(category =>
                category.Id == request.CategoryId && category.UserId == userId && category.Type == request.Type, cancellationToken))
        {
            return Result<RecurrenceResponse>.Failure("Select a category that matches the transaction type.");
        }

        var rule = new RecurringRule
        {
            UserId = userId,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Description = request.Description.Trim(),
            Type = request.Type,
            Amount = request.Amount,
            StartsOn = request.StartsOn,
            NextDate = request.StartsOn,
            Automatic = request.Automatic
        };

        context.RecurringRules.Add(rule);
        await context.SaveChangesAsync(cancellationToken);
        return Result<RecurrenceResponse>.Success(new(
            rule.Id, rule.Description, rule.AccountId, rule.CategoryId, rule.Type, rule.Amount,
            rule.StartsOn, rule.NextDate, rule.Automatic, rule.Paused));
    }

    public async Task<bool> SetPausedAsync(Guid userId, Guid id, bool paused, CancellationToken cancellationToken)
    {
        var rule = await context.RecurringRules.SingleOrDefaultAsync(
            rule => rule.Id == id && rule.UserId == userId, cancellationToken);
        if (rule is null)
        {
            return false;
        }

        if (rule.Paused && !paused && rule.NextDate < Today)
        {
            rule.NextDate = MonthlySchedule.OnOrAfter(rule.StartsOn, Today);
        }

        rule.Paused = paused;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var rule = await context.RecurringRules.SingleOrDefaultAsync(
            rule => rule.Id == id && rule.UserId == userId, cancellationToken);
        if (rule is null)
        {
            return false;
        }

        context.RecurringRules.Remove(rule);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<RecurrenceAgenda> GetAgendaAsync(Guid userId, CancellationToken cancellationToken)
    {
        var today = Today;
        var through = today.AddDays(30);
        await using var snapshot = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var items = await context.RecurringOccurrences.AsNoTracking()
            .Where(item => item.Rule.UserId == userId && item.PostedAt == null)
            .Select(item => new ScheduledOccurrence(item.RuleId, item.Rule.Description, item.Rule.AccountId,
                item.Rule.Type, item.Rule.Amount, item.Date, true, item.Rule.Automatic))
            .ToListAsync(cancellationToken);
        var rules = await context.RecurringRules.AsNoTracking()
            .Where(rule => rule.UserId == userId && !rule.Paused && rule.NextDate <= through)
            .ToListAsync(cancellationToken);
        var accounts = await context.Accounts.AsNoTracking()
            .Where(account => account.UserId == userId)
            .OrderBy(account => account.Name)
            .Select(account => new { account.Id, account.Name, account.CurrentBalance })
            .ToListAsync(cancellationToken);
        await snapshot.CommitAsync(cancellationToken);

        foreach (var rule in rules)
        {
            for (var date = rule.NextDate; date <= through; date = MonthlySchedule.Next(date, rule.StartsOn.Day))
            {
                items.Add(new(rule.Id, rule.Description, rule.AccountId, rule.Type, rule.Amount, date,
                    false, rule.Automatic));
            }
        }

        var byAccount = items.ToLookup(item => item.AccountId);
        var projections = accounts.Select(account =>
        {
            var scheduled = byAccount[account.Id];
            var delta = scheduled.Sum(item => (item.Type == TransactionType.Income ? 1 : -1) * (item.Amount ?? 0m));
            return new ProjectedAccount(account.Id, account.Name, account.CurrentBalance,
                account.CurrentBalance + delta, scheduled.Any(item => item.Amount is null));
        }).ToList();

        return new(today, through, items.OrderBy(item => item.Date).ThenBy(item => item.RuleId).ToList(), projections);
    }

    public async Task<Result> ConfirmAsync(
        Guid userId, Guid ruleId, DateOnly date, decimal? amount, CancellationToken cancellationToken)
    {
        var occurrence = await context.RecurringOccurrences.Include(item => item.Rule).SingleOrDefaultAsync(
            item => item.RuleId == ruleId && item.Date == date && item.Rule.UserId == userId, cancellationToken);
        if (occurrence is null || occurrence.Rule.Automatic)
        {
            return Result.Failure("Occurrence not found.");
        }

        if (occurrence.PostedAt is not null)
        {
            return Result.Success();
        }

        var value = amount ?? occurrence.Rule.Amount;
        if (value is null)
        {
            return Result.Failure("Enter the actual amount.");
        }

        return await PostAsync(occurrence, value.Value, cancellationToken);
    }

    public Task<List<Guid>> GetDueIdsAsync(CancellationToken cancellationToken)
    {
        var today = Today;
        return context.RecurringRules.AsNoTracking()
            .Where(rule => !rule.Paused && rule.NextDate <= today)
            .OrderBy(rule => rule.NextDate).ThenBy(rule => rule.Id)
            .Select(rule => rule.Id).ToListAsync(cancellationToken);
    }

    public async Task<Result> ProcessNextAsync(Guid id, CancellationToken cancellationToken)
    {
        var today = Today;
        var rule = await context.RecurringRules.SingleOrDefaultAsync(
            rule => rule.Id == id && !rule.Paused && rule.NextDate <= today, cancellationToken);
        if (rule is null)
        {
            return Result.Success();
        }

        var occurrence = new RecurringOccurrence { Rule = rule, RuleId = rule.Id, Date = rule.NextDate };
        context.RecurringOccurrences.Add(occurrence);
        rule.NextDate = MonthlySchedule.Next(rule.NextDate, rule.StartsOn.Day);

        if (rule.Automatic)
        {
            return await PostAsync(occurrence, rule.Amount!.Value, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result> PostAsync(RecurringOccurrence occurrence, decimal amount, CancellationToken cancellationToken)
    {
        var rule = occurrence.Rule;
        // The transaction service commits the account, ledger entry and occurrence in one EF save.
        occurrence.PostedAt = clock.GetUtcNow().UtcDateTime;
        var result = await transactions.CreateAsync(rule.UserId, new CreateTransactionRequest
        {
            AccountId = rule.AccountId,
            CategoryId = rule.CategoryId,
            Description = rule.Description,
            Type = rule.Type,
            Amount = amount,
            Date = occurrence.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
        }, cancellationToken);

        return result.IsSuccess
            ? Result.Success()
            : Result.Failure(result.Error ?? "The recurring transaction could not be posted.");
    }
}
