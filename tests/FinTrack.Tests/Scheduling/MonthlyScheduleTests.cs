using FinTrack.Application.Common;

namespace FinTrack.Tests.Scheduling;

public class MonthlyScheduleTests
{
    [Theory]
    [InlineData(2026, 28)]
    [InlineData(2028, 29)]
    public void Next_ClampsFebruaryAndRestoresTheOriginalDay(int year, int februaryDay)
    {
        var start = new DateOnly(year, 1, 31);
        var february = MonthlySchedule.Next(start, start.Day);

        Assert.Equal(new DateOnly(year, 2, februaryDay), february);
        Assert.Equal(new DateOnly(year, 3, 31), MonthlySchedule.Next(february, start.Day));
    }

    [Fact]
    public void Next_RollsIntoTheNextYear()
    {
        Assert.Equal(new DateOnly(2027, 1, 31), MonthlySchedule.Next(new DateOnly(2026, 12, 31), 31));
    }

    [Fact]
    public void OnOrAfter_PreservesAStartingDateInTheFuture()
    {
        var start = new DateOnly(2026, 12, 31);
        Assert.Equal(start, MonthlySchedule.OnOrAfter(start, new DateOnly(2026, 12, 1)));
    }

    [Fact]
    public void OnOrAfter_SkipsMissedMonthsButIncludesToday()
    {
        var start = new DateOnly(2026, 1, 31);
        var today = new DateOnly(2026, 4, 30);
        Assert.Equal(today, MonthlySchedule.OnOrAfter(start, today));
        Assert.Equal(new DateOnly(2026, 5, 31), MonthlySchedule.OnOrAfter(start, today.AddDays(1)));
    }
}
