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
    TimeZoneInfo timeZone,
    AmortizationIntegrity integrity) : IRecurrenceService
{
    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), timeZone).DateTime);

    public Task<List<RecurrenceResponse>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        context.RecurringRules.AsNoTracking()
            .Where(rule => rule.UserId == userId)
            .OrderBy(rule => rule.Paused).ThenBy(rule => rule.NextDate).ThenBy(rule => rule.Id)
            .Select(rule => new RecurrenceResponse(
                rule.Id, rule.Description, rule.AccountId, rule.CategoryId, rule.Type, rule.Amount,
                rule.StartsOn, rule.NextDate, rule.Automatic, rule.Paused, rule.PaymentMethod, rule.EndsOn))
            .ToListAsync(cancellationToken);

    public async Task<Result<RecurrenceResponse>> CreateAsync(
        Guid userId, CreateRecurrenceRequest request, CancellationToken cancellationToken)
    {
        var error = await ValidateReferencesAsync(userId, request, cancellationToken);
        if (error is not null)
            return Result<RecurrenceResponse>.Failure(error);

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
            EndsOn = request.EndsOn,
            PaymentMethod = request.PaymentMethod,
            Automatic = request.Automatic
        };

        rule.AmortizationSignature = integrity.Sign(rule);
        context.RecurringRules.Add(rule);
        await context.SaveChangesAsync(cancellationToken);
        return Result<RecurrenceResponse>.Success(new(
            rule.Id, rule.Description, rule.AccountId, rule.CategoryId, rule.Type, rule.Amount,
            rule.StartsOn, rule.NextDate, rule.Automatic, rule.Paused, rule.PaymentMethod, rule.EndsOn));
    }

    private async Task<string?> ValidateReferencesAsync(Guid userId, CreateRecurrenceRequest request, CancellationToken cancellationToken)
    {
        var accountType = await context.Accounts
            .Where(account => account.Id == request.AccountId && account.UserId == userId)
            .Select(account => (AccountType?)account.Type)
            .SingleOrDefaultAsync(cancellationToken);
        if (accountType is null)
        {
            return "Account not found.";
        }

        if (request.PaymentMethod == RecurringPaymentMethod.DirectDebit &&
            accountType is not (AccountType.Checking or AccountType.Savings))
        {
            return "Select a bank account for direct debit.";
        }

        if (!await context.Categories.AnyAsync(category =>
                category.Id == request.CategoryId && category.UserId == userId && category.Type == request.Type, cancellationToken))
        {
            return "Select a category that matches the transaction type.";
        }

        return null;
    }

    public async Task<Result> UpdateAsync(Guid userId, Guid id, CreateRecurrenceRequest request, CancellationToken cancellationToken)
    {
        var rule = await context.RecurringRules.SingleOrDefaultAsync(
            item => item.Id == id && item.UserId == userId, cancellationToken);
        if (rule is null)
            return Result.Failure("Schedule not found.");

        var error = await ValidateReferencesAsync(userId, request, cancellationToken);
        if (error is not null)
            return Result.Failure(error);

        var changedDate = request.StartsOn != rule.StartsOn;
        var nextDate = changedDate ? request.StartsOn : rule.NextDate;
        if (changedDate)
        {
            var latestOccurrence = await context.RecurringOccurrences.Where(item => item.RuleId == id)
                .MaxAsync(item => (DateOnly?)item.Date, cancellationToken);
            if (nextDate < Today || (latestOccurrence is DateOnly latest &&
                new DateOnly(nextDate.Year, nextDate.Month, 1) <= new DateOnly(latest.Year, latest.Month, 1)))
                return Result.Failure("Choose a future date after the month of the last generated installment.");
        }

        var pending = await context.RecurringOccurrences
            .Where(item => item.RuleId == id && item.PostedAt == null && item.OriginalAccountId == null)
            .ToListAsync(cancellationToken);
        foreach (var item in pending)
        {
            item.OriginalAccountId = rule.AccountId;
            item.OriginalCategoryId = rule.CategoryId;
            item.OriginalDescription = rule.Description;
            item.OriginalType = rule.Type;
            item.OriginalPaymentMethod = rule.PaymentMethod;
            item.OriginalAmount = rule.Amount;
        }

        rule.AccountId = request.AccountId;
        rule.CategoryId = request.CategoryId;
        rule.Description = request.Description.Trim();
        rule.Type = request.Type;
        rule.Amount = request.Amount;
        rule.StartsOn = request.StartsOn;
        rule.NextDate = nextDate;
        rule.EndsOn = request.EndsOn;
        rule.Automatic = request.Automatic;
        rule.PaymentMethod = request.PaymentMethod;
        rule.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<List<AmortizationResponse>?> ListAmortizationsAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var rule = await context.RecurringRules.AsNoTracking().Include(item => item.Amortizations)
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (rule is null)
            return null;

        integrity.Verify(rule);
        return rule.Amortizations.OrderByDescending(item => item.Date).ThenBy(item => item.Id)
            .Select(item => new AmortizationResponse(item.Id, item.Date, item.Amount, item.Installments,
                item.RemainingBefore, item.RemainingAfter, item.ComparedPayment, item.Savings)).ToList();
    }

    public async Task<Result> RecordAmortizationAsync(Guid userId, Guid id, RecordAmortizationRequest request, CancellationToken cancellationToken)
    {
        var rule = await context.RecurringRules.Include(item => item.Amortizations).SingleOrDefaultAsync(
            item => item.Id == id && item.UserId == userId, cancellationToken);
        if (rule is null || rule.Type != TransactionType.Expense)
            return Result.Failure("Choose one of your recurring expenses.");

        integrity.Verify(rule);

        var existing = await context.RecurrenceAmortizations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken);
        if (existing is not null)
            return existing.RuleId == id && existing.Amount == request.Amount &&
                existing.Date == request.Date && existing.Installments == request.Installments
                ? Result.Success() : Result.Failure("This amortization request has already been used.");

        if (request.Date > Today)
            return Result.Failure("Enter the date the amortization was paid.");

        if (request.Installments > 0)
        {
            if (rule.EndsOn is not DateOnly end)
                return Result.Failure("Set the final installment date before reducing the term.");

            var firstFuture = rule.NextDate > Today ? rule.NextDate : MonthlySchedule.OnOrAfter(rule.StartsOn, Today.AddDays(1));
            var finalDate = MonthlySchedule.OnOrBefore(end, rule.StartsOn.Day);
            var remaining = (finalDate.Year - firstFuture.Year) * 12 + finalDate.Month - firstFuture.Month + 1;
            if (finalDate < firstFuture || request.Installments > remaining)
                return Result.Failure("The number exceeds the remaining future installments.");

            var firstRemovedMonth = new DateOnly(finalDate.Year, finalDate.Month, 1).AddMonths(1 - request.Installments);
            var firstRemoved = MonthlySchedule.OnOrAfter(rule.StartsOn, firstRemovedMonth);
            var cutoff = firstRemoved.AddDays(-1);
            rule.EndsOn = cutoff < rule.StartsOn ? cutoff : MonthlySchedule.OnOrBefore(cutoff, rule.StartsOn.Day);
        }

        rule.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        var amortization = new RecurrenceAmortization
        {
            Id = request.Id, RuleId = id, Date = request.Date, Amount = request.Amount, Installments = request.Installments
        };

        rule.Amortizations.Add(amortization);
        context.RecurrenceAmortizations.Add(amortization);
        rule.AmortizationSignature = integrity.Sign(rule);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> SaveAmortizationComparisonAsync(Guid userId, Guid id, Guid amortizationId,
        AmortizationComparison request, CancellationToken cancellationToken)
    {
        var rule = await context.RecurringRules.Include(item => item.Amortizations)
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (rule is not null) integrity.Verify(rule);
        var item = rule?.Amortizations.SingleOrDefault(entry => entry.Id == amortizationId);
        if (rule is null || item is null)
            return Result.Failure("Amortization not found.");

        if (item.Amount is decimal paid && request.Payment != paid)
            return Result.Failure("The compared payment must match the amount recorded for the amortization.");

        ApplyComparison(item, request);
        rule.AmortizationSignature = integrity.Sign(rule);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static void ApplyComparison(RecurrenceAmortization item, AmortizationComparison comparison)
    {
        item.RemainingBefore = comparison.RemainingBefore;
        item.RemainingAfter = comparison.RemainingAfter;
        item.ComparedPayment = comparison.Payment;
        item.Savings = comparison.RemainingBefore - comparison.Payment - comparison.RemainingAfter;
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
        var rule = await context.RecurringRules.Include(item => item.Amortizations).SingleOrDefaultAsync(
            rule => rule.Id == id && rule.UserId == userId, cancellationToken);
        if (rule is null)
        {
            return false;
        }

        integrity.Verify(rule);
        if (rule.Amortizations.Count > 0)
            throw new AmortizationHistoryExistsException();
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
            .Select(item => new ScheduledOccurrence(item.RuleId, item.OriginalDescription ?? item.Rule.Description,
                item.OriginalAccountId ?? item.Rule.AccountId, item.OriginalType ?? item.Rule.Type,
                item.OriginalAccountId == null ? item.Rule.Amount : item.OriginalAmount,
                item.Date, true, false, item.OriginalAccountId == null ? item.Rule.PaymentMethod : item.OriginalPaymentMethod))
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
            for (var date = rule.NextDate; date <= through && (rule.EndsOn == null || date <= rule.EndsOn); date = MonthlySchedule.Next(date, rule.StartsOn.Day))
            {
                items.Add(new(rule.Id, rule.Description, rule.AccountId, rule.Type, rule.Amount, date,
                    false, rule.Automatic, rule.PaymentMethod));
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
        if (occurrence is null)
        {
            return Result.Failure("Occurrence not found.");
        }

        if (occurrence.PostedAt is not null)
        {
            return Result.Success();
        }

        if (occurrence.Rule.Automatic && occurrence.OriginalAccountId == null)
            return Result.Failure("Occurrence not found.");

        var value = amount ?? (occurrence.OriginalAccountId == null ? occurrence.Rule.Amount : occurrence.OriginalAmount);
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
            .Where(rule => !rule.Paused && rule.NextDate <= today && (rule.EndsOn == null || rule.NextDate <= rule.EndsOn))
            .OrderBy(rule => rule.NextDate).ThenBy(rule => rule.Id)
            .Select(rule => rule.Id).ToListAsync(cancellationToken);
    }

    public async Task<Result> ProcessNextAsync(Guid id, CancellationToken cancellationToken)
    {
        var today = Today;
        var rule = await context.RecurringRules.SingleOrDefaultAsync(
            rule => rule.Id == id && !rule.Paused && rule.NextDate <= today && (rule.EndsOn == null || rule.NextDate <= rule.EndsOn), cancellationToken);
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
        var request = new CreateTransactionRequest
        {
            AccountId = occurrence.OriginalAccountId ?? rule.AccountId,
            CategoryId = occurrence.OriginalCategoryId ?? rule.CategoryId,
            Description = occurrence.OriginalDescription ?? rule.Description,
            Type = occurrence.OriginalType ?? rule.Type,
            Amount = amount,
            Date = occurrence.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
        };
        occurrence.OriginalAccountId = null;
        occurrence.OriginalCategoryId = null;
        occurrence.OriginalDescription = null;
        occurrence.OriginalType = null;
        occurrence.OriginalPaymentMethod = null;
        occurrence.OriginalAmount = null;
        var result = await transactions.CreateAsync(rule.UserId, request, cancellationToken);

        return result.IsSuccess
            ? Result.Success()
            : Result.Failure(result.Error ?? "The recurring transaction could not be posted.");
    }
}
