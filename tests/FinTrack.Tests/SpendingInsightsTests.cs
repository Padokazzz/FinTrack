using FinTrack.Infrastructure.Insights;

namespace FinTrack.Tests;

public class SpendingInsightsTests
{
    [Theory]
    [InlineData(2026, 11, 23, 2026, 11, 23)]
    [InlineData(2026, 11, 29, 2026, 11, 23)]
    [InlineData(2027, 1, 1, 2026, 12, 28)]
    public void WeekStartsOnMonday(int year, int month, int day, int expectedYear, int expectedMonth, int expectedDay)
    {
        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay),
            SpendingInsights.WeekStart(new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData(79.99, "under")]
    [InlineData(80, "near")]
    [InlineData(99.99, "near")]
    [InlineData(100, "reached")]
    [InlineData(101, "reached")]
    public void BudgetStatusUsesExactBoundaries(decimal amount, string expected)
    {
        Assert.Equal(expected, new BudgetUsage(null, null, amount, 100m).Status);
    }

    [Fact]
    public void SummarizeComparesConsecutiveWeeksAndExcludesTheNextMonday()
    {
        var category = Guid.NewGuid();
        var start = new DateOnly(2026, 12, 28);
        var monday = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var result = SpendingInsights.Summarize(start, [
            new(monday.AddDays(-1), category, "Meals", 40m, 1, 0, 0m),
            new(monday, category, "Meals", 30m, 2, 2, 30m),
            new(monday.AddDays(6), category, "Meals", 50m, 1, 0, 0m),
            new(monday.AddDays(7), category, "Meals", 200m, 1, 0, 0m),
        ]);

        Assert.Equal(new DateOnly(2027, 1, 3), result.End);
        Assert.Equal(80m, result.Total);
        Assert.Equal(40m, result.PreviousTotal);
        Assert.Equal(3, result.Count);
        Assert.Equal(2, result.SmallCount);
        Assert.Equal(30m, result.SmallTotal);
        Assert.Equal(40m, Assert.Single(result.Categories).PreviousAmount);
    }
}
