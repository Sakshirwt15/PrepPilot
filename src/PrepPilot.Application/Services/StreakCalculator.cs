namespace PrepPilot.Application.Services;

public static class StreakCalculator
{
    /// <summary>
    /// Number of consecutive active days ending today. If there is no activity today yet,
    /// a streak that ended yesterday still counts (so it does not reset until a day is missed).
    /// </summary>
    public static int Calculate(IEnumerable<DateOnly> activeDays, DateOnly today)
    {
        var days = new HashSet<DateOnly>(activeDays);
        var cursor = days.Contains(today) ? today : today.AddDays(-1);

        var streak = 0;
        while (days.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }
        return streak;
    }
}
