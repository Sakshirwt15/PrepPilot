using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PrepPilot.Application.Abstractions;
using PrepPilot.Application.Dtos;
using PrepPilot.Application.Services;
using PrepPilot.Domain.Entities;
using PrepPilot.Infrastructure.Persistence;
using PrepPilot.Infrastructure.Security;

namespace PrepPilot.Tests;

/// <summary>Stands in for the real AI so tests never hit the network or need an API key.</summary>
public class FakeAiClient(string response = "{}") : IAiClient
{
    public int Calls { get; private set; }
    public string? LastSystemPrompt { get; private set; }
    public string? LastUserPrompt { get; private set; }
    public bool? LastExpectJson { get; private set; }

    public Task<string> CompleteAsync(string systemPrompt, string userPrompt, bool expectJson, CancellationToken ct = default)
    {
        Calls++;
        LastSystemPrompt = systemPrompt;
        LastUserPrompt = userPrompt;
        LastExpectJson = expectJson;
        return Task.FromResult(response);
    }
}

internal static class TestSupport
{
    /// <summary>A fresh, isolated in-memory database.</summary>
    public static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>In-memory database filled by the real seeder (companies, papers and DSA problems).</summary>
    public static async Task<AppDbContext> SeededDbAsync()
    {
        var db = NewDb();
        await DatabaseInitializer.InitializeAsync(db, seed: true);
        return db;
    }

    public static async Task<User> AddUserAsync(AppDbContext db, string name = "Test User", string? email = null)
    {
        var user = new User
        {
            FullName = name,
            Email = email ?? $"{Guid.NewGuid():N}@example.com",
            PasswordHash = "x",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public static async Task<MockTest> LoadTestAsync(AppDbContext db, string titlePrefix) =>
        await db.MockTests
            .Include(t => t.Questions).ThenInclude(q => q.Options)
            .FirstAsync(t => t.Title.StartsWith(titlePrefix));

    /// <summary>Answers for every question: the correct option, or (when correct = false) a wrong one.</summary>
    public static SubmitAttemptDto AnswerAll(MockTest test, bool correct) => new()
    {
        Answers = test.Questions.Select(q => new AnswerDto
        {
            QuestionId = q.Id,
            OptionId = q.Options.First(o => o.IsCorrect == correct).Id
        }).ToList()
    };

    /// <summary>Valid AI paper JSON with the requested number of questions.</summary>
    public static string PaperJson(int count, string topic = "Quantitative Aptitude") =>
        JsonSerializer.Serialize(new
        {
            title = "AI Paper",
            description = "Generated for tests",
            questions = Enumerable.Range(1, count).Select(i => new
            {
                text = $"Question {i}?",
                topic,
                points = 1,
                explanation = "Because.",
                options = new[]
                {
                    new { text = "A", isCorrect = false },
                    new { text = "B", isCorrect = true },
                    new { text = "C", isCorrect = false },
                    new { text = "D", isCorrect = false }
                }
            }).ToList()
        });

    // ------- service factories wired like production, but over the in-memory database -------
    public static AuthService Auth(AppDbContext db) => new(
        new UserRepository(db), new BCryptPasswordHasher(),
        new JwtTokenService(Options.Create(new JwtOptions { Key = new string('k', 40) }), TimeProvider.System),
        db, TimeProvider.System);

    public static AttemptService Attempts(AppDbContext db) =>
        new(new MockTestRepository(db), new AttemptRepository(db), db, TimeProvider.System);

    public static ProblemService Problems(AppDbContext db) =>
        new(new ProblemRepository(db), db, TimeProvider.System);

    public static AiPaperService Paper(AppDbContext db, IAiClient ai) =>
        new(ai, new CompanyRepository(db), new MockTestRepository(db), db, TimeProvider.System);

    public static AiCoachService Coach(AppDbContext db, IAiClient ai) =>
        new(ai, new ProblemRepository(db));

    public static DashboardService Dashboard(AppDbContext db) => new(
        new UserRepository(db), new AttemptRepository(db), new ProblemRepository(db),
        new StatsRepository(db), TimeProvider.System);
}
