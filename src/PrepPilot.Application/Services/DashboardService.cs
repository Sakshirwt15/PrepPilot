using PrepPilot.Application.Abstractions;
using PrepPilot.Application.Dtos;
using PrepPilot.Application.Exceptions;

namespace PrepPilot.Application.Services;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(int userId, CancellationToken ct = default);
    Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int? currentUserId, CancellationToken ct = default);
}

public class DashboardService(
    IUserRepository users,
    IAttemptRepository attempts,
    IProblemRepository problems,
    IStatsRepository stats,
    TimeProvider time) : IDashboardService
{
    public async Task<DashboardDto> GetAsync(int userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("User not found.");

        var history = (await attempts.ListSubmittedForUserAsync(userId, 200, ct)).Select(AttemptService.ToHistory).ToList();
        var allProblems = await problems.ListAsync(ct);
        var solvedIds = await problems.GetSolvedProblemIdsAsync(userId, ct);
        var topicRows = await stats.GetTopicAnswersAsync(userId, ct);
        var activity = await stats.GetActivityTimesAsync(userId, ct);

        var topics = topicRows
            .GroupBy(r => r.Topic)
            .Select(g =>
            {
                var correct = g.Count(r => r.IsCorrect);
                return new TopicStatDto(g.Key, correct, g.Count(), Math.Round(correct * 100.0 / g.Count(), 1));
            })
            .OrderBy(t => t.Topic)
            .ToList();

        // Weak topics need enough answers to be meaningful.
        var weak = topics.Where(t => t.Total >= 3 && t.Percent < 70)
            .OrderBy(t => t.Percent).Take(3).Select(t => t.Topic).ToList();

        int SolvedOf(string difficulty) => allProblems.Count(p => p.Difficulty == difficulty && solvedIds.Contains(p.Id));
        int TotalOf(string difficulty) => allProblems.Count(p => p.Difficulty == difficulty);

        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var streak = StreakCalculator.Calculate(activity.Select(DateOnly.FromDateTime), today);

        return new DashboardDto(
            user.FullName,
            history.Count,
            history.Count == 0 ? 0 : Math.Round(history.Average(h => h.Percent), 1),
            history.Count == 0 ? 0 : history.Max(h => h.Percent),
            solvedIds.Count,
            allProblems.Count,
            new SolvedByDifficultyDto(SolvedOf("Easy"), SolvedOf("Medium"), SolvedOf("Hard"),
                TotalOf("Easy"), TotalOf("Medium"), TotalOf("Hard")),
            streak,
            topics,
            weak,
            history.Take(8).ToList());
    }

    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int? currentUserId, CancellationToken ct = default)
    {
        var rows = await stats.GetLeaderboardRowsAsync(ct);

        return rows
            .Select(r => new { Row = r, Points = r.ProblemsSolved * 10 + r.MockTestsTaken * 5 })
            .Where(x => x.Points > 0)
            .OrderByDescending(x => x.Points).ThenBy(x => x.Row.FullName)
            .Take(20)
            .Select((x, index) => new LeaderboardEntryDto(
                index + 1, MaskName(x.Row.FullName), x.Row.ProblemsSolved, x.Row.MockTestsTaken,
                Math.Round(x.Row.AveragePercent, 1), x.Points, currentUserId == x.Row.UserId))
            .ToList();
    }

    /// <summary>Public boards show "Sakshi R." rather than full names.</summary>
    internal static string MaskName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length <= 1 ? fullName.Trim() : $"{parts[0]} {parts[^1][0]}.";
    }
}
