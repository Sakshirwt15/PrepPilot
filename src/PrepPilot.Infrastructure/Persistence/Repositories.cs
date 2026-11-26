using Microsoft.EntityFrameworkCore;
using PrepPilot.Application.Abstractions;
using PrepPilot.Domain.Entities;

namespace PrepPilot.Infrastructure.Persistence;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await db.Users.AddAsync(user, ct);
}

public class CompanyRepository(AppDbContext db) : ICompanyRepository
{
    public Task<List<Company>> ListAsync(CancellationToken ct = default) =>
        db.Companies.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public Task<Company?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        db.Companies.FirstOrDefaultAsync(c => c.Slug == slug, ct);
}

public class MockTestRepository(AppDbContext db) : IMockTestRepository
{
    public Task<List<MockTest>> ListAsync(int? companyId, string? difficulty, CancellationToken ct = default)
    {
        var query = db.MockTests.AsNoTracking().Include(t => t.Company).Include(t => t.Questions).AsQueryable();

        if (companyId is not null) query = query.Where(t => t.CompanyId == companyId);
        if (difficulty is not null) query = query.Where(t => t.Difficulty == difficulty);

        return query.OrderBy(t => t.Title).ToListAsync(ct);
    }

    public Task<MockTest?> GetWithQuestionsAsync(int id, CancellationToken ct = default) =>
        db.MockTests
            .Include(t => t.Company)
            .Include(t => t.Questions).ThenInclude(q => q.Options)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAsync(MockTest test, CancellationToken ct = default) =>
        await db.MockTests.AddAsync(test, ct);

    public Task<int> CountAiGeneratedSinceAsync(int userId, DateTime sinceUtc, CancellationToken ct = default) =>
        db.MockTests.CountAsync(t => t.IsAiGenerated && t.CreatedByUserId == userId && t.CreatedAtUtc >= sinceUtc, ct);

    public Task<Dictionary<int, int>> CountByCompanyAsync(CancellationToken ct = default) =>
        db.MockTests
            .Where(t => t.CompanyId != null)
            .GroupBy(t => t.CompanyId!.Value)
            .Select(g => new { CompanyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CompanyId, x => x.Count, ct);
}

public class AttemptRepository(AppDbContext db) : IAttemptRepository
{
    public Task<Attempt?> GetAsync(int id, CancellationToken ct = default) =>
        db.Attempts.Include(a => a.Answers).FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task AddAsync(Attempt attempt, CancellationToken ct = default) =>
        await db.Attempts.AddAsync(attempt, ct);

    public Task<List<Attempt>> ListSubmittedForUserAsync(int userId, int take, CancellationToken ct = default) =>
        db.Attempts
            .AsNoTracking()
            .Include(a => a.MockTest).ThenInclude(t => t!.Company)
            .Where(a => a.UserId == userId && a.SubmittedAtUtc != null)
            .OrderByDescending(a => a.SubmittedAtUtc)
            .Take(take)
            .ToListAsync(ct);
}

public class ProblemRepository(AppDbContext db) : IProblemRepository
{
    public Task<List<Problem>> ListAsync(CancellationToken ct = default) =>
        db.Problems.AsNoTracking().ToListAsync(ct);

    public Task<Problem?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        db.Problems.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == slug, ct);

    public Task<Problem?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Problems.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<HashSet<int>> GetSolvedProblemIdsAsync(int userId, CancellationToken ct = default)
    {
        var ids = await db.ProblemSubmissions
            .Where(s => s.UserId == userId && s.Passed)
            .Select(s => s.ProblemId)
            .Distinct()
            .ToListAsync(ct);
        return ids.ToHashSet();
    }

    public async Task AddSubmissionAsync(ProblemSubmission submission, CancellationToken ct = default) =>
        await db.ProblemSubmissions.AddAsync(submission, ct);

    public Task<List<ProblemSubmission>> GetSubmissionsAsync(int problemId, int userId, int take, CancellationToken ct = default) =>
        db.ProblemSubmissions
            .AsNoTracking()
            .Where(s => s.ProblemId == problemId && s.UserId == userId)
            .OrderByDescending(s => s.SubmittedAtUtc)
            .Take(take)
            .ToListAsync(ct);
}

public class StatsRepository(AppDbContext db) : IStatsRepository
{
    public async Task<List<TopicAnswerRow>> GetTopicAnswersAsync(int userId, CancellationToken ct = default)
    {
        var rows = await (
            from answer in db.AttemptAnswers
            join question in db.Questions on answer.QuestionId equals question.Id
            where answer.Attempt!.UserId == userId
            select new { question.Topic, answer.IsCorrect }).ToListAsync(ct);

        return rows.Select(r => new TopicAnswerRow(r.Topic, r.IsCorrect)).ToList();
    }

    public async Task<List<DateTime>> GetActivityTimesAsync(int userId, CancellationToken ct = default)
    {
        var attemptTimes = await db.Attempts
            .Where(a => a.UserId == userId && a.SubmittedAtUtc != null)
            .Select(a => a.SubmittedAtUtc!.Value)
            .ToListAsync(ct);

        var submissionTimes = await db.ProblemSubmissions
            .Where(s => s.UserId == userId)
            .Select(s => s.SubmittedAtUtc)
            .ToListAsync(ct);

        return attemptTimes.Concat(submissionTimes).ToList();
    }

    public async Task<List<LeaderboardRow>> GetLeaderboardRowsAsync(CancellationToken ct = default)
    {
        var solved = await db.ProblemSubmissions
            .Where(s => s.Passed)
            .Select(s => new { s.UserId, s.ProblemId })
            .Distinct()
            .ToListAsync(ct);
        var solvedByUser = solved.GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.Count());

        var attempts = await db.Attempts
            .Where(a => a.SubmittedAtUtc != null)
            .Select(a => new { a.UserId, a.Score, a.MaxScore })
            .ToListAsync(ct);
        var testsByUser = attempts.GroupBy(a => a.UserId).ToDictionary(
            g => g.Key,
            g => (Count: g.Count(), Average: g.Average(a => a.MaxScore == 0 ? 0.0 : a.Score * 100.0 / a.MaxScore)));

        var userIds = solvedByUser.Keys.Union(testsByUser.Keys).ToList();
        var users = await db.Users.Where(u => userIds.Contains(u.Id)).Select(u => new { u.Id, u.FullName }).ToListAsync(ct);

        return users.Select(u => new LeaderboardRow(
            u.Id,
            u.FullName,
            solvedByUser.TryGetValue(u.Id, out var s) ? s : 0,
            testsByUser.TryGetValue(u.Id, out var t) ? t.Count : 0,
            testsByUser.TryGetValue(u.Id, out var t2) ? t2.Average : 0)).ToList();
    }
}
