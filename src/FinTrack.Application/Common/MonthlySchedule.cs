namespace FinTrack.Application.Common;

public static class MonthlySchedule
{
    public static DateOnly Next(DateOnly date, int dayOfMonth)
    {
        var firstOfNextMonth = new DateOnly(date.Year, date.Month, 1).AddMonths(1);
        return InMonth(firstOfNextMonth, dayOfMonth);
    }

    public static DateOnly OnOrAfter(DateOnly start, DateOnly date)
    {
        if (date <= start)
        {
            return start;
        }

        var candidate = InMonth(date, start.Day);
        return candidate < date ? Next(date, start.Day) : candidate;
    }

    public static DateOnly OnOrBefore(DateOnly date, int dayOfMonth)
    {
        var candidate = InMonth(date, dayOfMonth);
        return candidate <= date ? candidate : InMonth(date.AddMonths(-1), dayOfMonth);
    }

    private static DateOnly InMonth(DateOnly month, int dayOfMonth) =>
        new(month.Year, month.Month, Math.Min(dayOfMonth, DateTime.DaysInMonth(month.Year, month.Month)));
}
