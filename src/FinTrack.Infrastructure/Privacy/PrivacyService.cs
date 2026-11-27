using System.Data;
using FinTrack.Application.Interfaces.Security;
using FinTrack.Infrastructure.Data;
using FinTrack.Infrastructure.Insights;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Privacy;

public sealed class PrivacyService(FinTrackDbContext db, IPasswordHasher passwords, SpendingInsights insights)
{
    public async Task<object?> ExportAsync(Guid userId, string password, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null || !passwords.Verify(password, user.PasswordHash))
            return null;

        var accounts = await db.Accounts.Where(x => x.UserId == userId)
            .Select(x => new { x.Id, x.Name, x.Type, x.InitialBalance, x.CurrentBalance, x.CreatedAt, x.UpdatedAt })
            .ToListAsync(ct);
        var categories = await db.Categories.Where(x => x.UserId == userId)
            .Select(x => new { x.Id, x.Name, x.Type, x.CreatedAt, x.UpdatedAt }).ToListAsync(ct);
        var transactions = await db.Transactions.Where(x => x.UserId == userId)
            .Select(x => new { x.Id, x.Description, x.Amount, x.Type, x.Date, x.AccountId, x.CategoryId, x.CreatedAt, x.UpdatedAt })
            .ToListAsync(ct);
        var transfers = await db.Transfers.Where(x => x.UserId == userId)
            .Select(x => new { x.Id, x.Description, x.Amount, x.Date, x.FromAccountId, x.ToAccountId, x.CreatedAt, x.UpdatedAt })
            .ToListAsync(ct);
        var recurrences = await db.RecurringRules.Where(x => x.UserId == userId)
            .Select(x => new
            {
                x.Id, x.AccountId, x.CategoryId, x.Description, x.Type, x.Amount, x.StartsOn,
                x.NextDate, x.Automatic, x.Paused, x.CreatedAt, x.UpdatedAt
            }).ToListAsync(ct);
        var occurrences = await db.RecurringOccurrences.Where(x => x.Rule.UserId == userId)
            .Select(x => new { x.RuleId, x.Date, x.PostedAt }).ToListAsync(ct);
        var emailVerification = await db.EmailVerifications.Where(x => x.UserId == userId)
            .Select(x => new { x.VerifiedAt, x.LastActivityAt }).SingleOrDefaultAsync(ct);
        var limits = await insights.GetLimitsAsync(userId, ct);

        await transaction.CommitAsync(ct);
        return new
        {
            schemaVersion = 1,
            profile = new { user.Id, user.Name, user.Email, user.CreatedAt, user.UpdatedAt },
            accounts, categories, transactions, transfers, recurrences, occurrences, limits, emailVerification
        };
    }

    public async Task<bool> DeleteAsync(Guid userId, string password, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var user = await db.Users.FromSqlInterpolated(
            $"""SELECT * FROM "Users" WHERE "Id" = {userId} FOR UPDATE""")
            .AsNoTracking().SingleOrDefaultAsync(ct);
        if (user is null || !passwords.Verify(password, user.PasswordHash))
            return false;

        // Remove rows with restrictive account and category foreign keys before deleting the user.
        await db.RecurringRules.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await db.Transfers.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await db.Transactions.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await db.Users.Where(x => x.Id == userId).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }
}
