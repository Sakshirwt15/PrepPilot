using PrepPilot.Domain.Entities;

namespace PrepPilot.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public record AuthToken(string Token, DateTime ExpiresAtUtc);

public interface ITokenService
{
    AuthToken Create(User user);
}

/// <summary>Provider-neutral text generation (Groq, Anthropic, ...).</summary>
public interface IAiClient
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, bool expectJson, CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}

public interface ICompanyRepository
{
    Task<List<Company>> ListAsync(CancellationToken ct = default);
    Task<Company?> GetBySlugAsync(string slug, CancellationToken ct = default);
}

public interface IMockTestRepository
{
    Task<List<MockTest>> ListAsync(int? companyId, string? difficulty, CancellationToken ct = default);
    Task<MockTest?> GetWithQuestionsAsync(int id, CancellationToken ct = default);
    Task AddAsync(MockTest test, CancellationToken ct = default);
    Task<int> CountAiGeneratedSinceAsync(int userId, DateTime sinceUtc, CancellationToken ct = default);
    Task<Dictionary<int, int>> CountByCompanyAsync(CancellationToken ct = default);
}

public interface IAttemptRepository
{
    /// <summary>Loads an attempt with its answers (tracked).</summary>
    Task<Attempt?> GetAsync(int id, CancellationToken ct = default);
    Task AddAsync(Attempt attempt, CancellationToken ct = default);
    Task<List<Attempt>> ListSubmittedForUserAsync(int userId, int take, CancellationToken ct = default);
}

public interface IProblemRepository
{
    Task<List<Problem>> ListAsync(CancellationToken ct = default);
    Task<Problem?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Problem?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<HashSet<int>> GetSolvedProblemIdsAsync(int userId, CancellationToken ct = default);
    Task AddSubmissionAsync(ProblemSubmission submission, CancellationToken ct = default);
    Task<List<ProblemSubmission>> GetSubmissionsAsync(int problemId, int userId, int take, CancellationToken ct = default);
}

public record TopicAnswerRow(string Topic, bool IsCorrect);

public record LeaderboardRow(int UserId, string FullName, int ProblemsSolved, int MockTestsTaken, double AveragePercent);

public interface IStatsRepository
{
    Task<List<TopicAnswerRow>> GetTopicAnswersAsync(int userId, CancellationToken ct = default);
    Task<List<DateTime>> GetActivityTimesAsync(int userId, CancellationToken ct = default);
    Task<List<LeaderboardRow>> GetLeaderboardRowsAsync(CancellationToken ct = default);
}
