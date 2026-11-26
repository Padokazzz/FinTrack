using System.Text.Json;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Insights;

public record CategoryLimit(Guid CategoryId, decimal Limit);
public record SpendingLimits(decimal? OverallLimit, CategoryLimit[] Categories);
public record BudgetUsage(Guid? CategoryId, string? Category, decimal Amount, decimal Limit)
{
    public string Status => Amount >= Limit ? "reached" : Amount >= Limit * 0.8m ? "near" : "under";
}
public record CategorySpending(Guid CategoryId, string Category, decimal Amount, decimal PreviousAmount, int Count);
public record WeeklySpending(DateOnly Start, DateOnly End, decimal Total, decimal PreviousTotal,
    int Count, int SmallCount, decimal SmallTotal, CategorySpending[] Categories);

public record ExpenseCategory(Guid Id, string Name);
public record SpendingOverview(DateOnly Month, IReadOnlyList<BudgetUsage> Budgets,
    IReadOnlyList<ExpenseCategory> Categories, WeeklySpending[] Weeks, decimal SmallPurchaseLimit);

public class SpendingInsights(FinTrackDbContext db, TimeProvider clock, TimeZoneInfo timeZone)
{
    public const decimal SmallPurchaseLimit = 30m;
    private static readonly SpendingLimits EmptyLimits = new(null, []);

    public async Task<SpendingLimits> GetLimitsAsync(Guid userId, CancellationToken ct)
    {
        var settings = await db.SpendingBudgets.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        return settings is null ? EmptyLimits : JsonSerializer.Deserialize<SpendingLimits>(settings.Payload)!;
    }

    public async Task<bool> SaveAsync(Guid userId, SpendingLimits limits, CancellationToken ct)
    {
        static bool Valid(decimal amount) => amount > 0 && amount <= 999_999_999m && decimal.Round(amount, 2) == amount;

        if ((limits.OverallLimit is decimal overall && !Valid(overall)) ||
            limits.Categories is null || limits.Categories.Any(x => x is null || !Valid(x.Limit)))
            return false;

        var ids = limits.Categories.Select(x => x.CategoryId).ToArray();
        var ownedCount = await db.Categories.CountAsync(x => x.UserId == userId &&
            x.Type == TransactionType.Expense && ids.Contains(x.Id), ct);
        if (ownedCount != ids.Length)
            return false;

        var payload = JsonSerializer.Serialize(limits);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "SpendingBudgets" ("UserId", "Payload") VALUES ({userId}, {payload})
            ON CONFLICT ("UserId") DO UPDATE SET "Payload" = EXCLUDED."Payload"
            """, ct);
        return true;
    }

    public async Task<SpendingOverview> GetAsync(Guid userId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), timeZone).DateTime);
        var month = new DateOnly(today.Year, today.Month, 1);
        var week = WeekStart(today);
        var from = Utc(week.AddDays(-63));
        var through = Utc(today.AddDays(1));
        var daily = await db.Transactions.AsNoTracking()
            .Where(x => x.UserId == userId && x.Type == TransactionType.Expense && x.Date >= from && x.Date < through)
            .GroupBy(x => new { Day = x.Date.Date, x.CategoryId, Category = x.Category.Name })
            .Select(g => new DailySpending(g.Key.Day, g.Key.CategoryId, g.Key.Category, g.Sum(x => x.Amount),
                g.Count(), g.Count(x => x.Amount <= SmallPurchaseLimit),
                g.Sum(x => x.Amount <= SmallPurchaseLimit ? x.Amount : 0m)))
            .ToListAsync(ct);

        var limits = await GetLimitsAsync(userId, ct);
        var categories = await db.Categories.Where(x => x.UserId == userId && x.Type == TransactionType.Expense)
            .OrderBy(x => x.Name).Select(x => new ExpenseCategory(x.Id, x.Name)).ToListAsync(ct);
        var monthly = daily.Where(x => x.Day >= Utc(month)).ToArray();
        var budgets = new List<BudgetUsage>();
        if (limits.OverallLimit is decimal overall)
            budgets.Add(new(null, null, monthly.Sum(x => x.Amount), overall));
        var categoryLimits = limits.Categories.ToDictionary(x => x.CategoryId, x => x.Limit);
        foreach (var category in categories)
        {
            if (categoryLimits.TryGetValue(category.Id, out var limit))
                budgets.Add(new(category.Id, category.Name, monthly.Where(x => x.CategoryId == category.Id).Sum(x => x.Amount), limit));
        }

        var weeks = Enumerable.Range(1, 8).Select(offset => Summarize(week.AddDays(-7 * offset), daily)).ToArray();
        return new(month, budgets, categories, weeks, SmallPurchaseLimit);
    }

    public static DateOnly WeekStart(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    public static WeeklySpending Summarize(DateOnly start, IReadOnlyList<DailySpending> daily)
    {
        var lower = Utc(start);
        var upper = Utc(start.AddDays(7));
        var current = daily.Where(x => x.Day >= lower && x.Day < upper).ToArray();
        var previous = daily.Where(x => x.Day >= lower.AddDays(-7) && x.Day < lower).ToArray();
        var previousByCategory = previous.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
        var categories = current.GroupBy(x => new { x.CategoryId, x.Category })
            .Select(g => new CategorySpending(g.Key.CategoryId, g.Key.Category, g.Sum(x => x.Amount),
                previousByCategory.GetValueOrDefault(g.Key.CategoryId), g.Sum(x => x.Count)))
            .OrderByDescending(x => x.Amount).ThenBy(x => x.Category).ToArray();

        return new(start, start.AddDays(6), current.Sum(x => x.Amount), previous.Sum(x => x.Amount),
            current.Sum(x => x.Count), current.Sum(x => x.SmallCount), current.Sum(x => x.SmallTotal), categories);
    }

    private static DateTime Utc(DateOnly date) => date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}

public record DailySpending(DateTime Day, Guid CategoryId, string Category, decimal Amount,
    int Count, int SmallCount, decimal SmallTotal);
