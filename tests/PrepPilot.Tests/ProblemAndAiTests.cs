using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PrepPilot.Application.Dtos;
using PrepPilot.Application.Exceptions;
using PrepPilot.Application.Services;

namespace PrepPilot.Tests;

public class ProblemServiceTests
{
    private static async Task<(PrepPilot.Infrastructure.Persistence.AppDbContext Db, int UserId, int ProblemId, int Tests)> SetupAsync(string slug = "two-sum")
    {
        var db = await TestSupport.SeededDbAsync();
        var user = await TestSupport.AddUserAsync(db);
        var problem = await db.Problems.FirstAsync(p => p.Slug == slug);
        var total = (await TestSupport.Problems(db).GetAsync(user.Id, slug)).TestCases.GetArrayLength();
        return (db, user.Id, problem.Id, total);
    }

    [Fact]
    public async Task Detail_ContainsRunnableTestCases()
    {
        var (db, userId, _, total) = await SetupAsync();
        using var dbScope = db;

        var detail = await TestSupport.Problems(db).GetAsync(userId, "two-sum");

        Assert.Equal("twoSum", detail.FunctionName);
        Assert.True(total >= 5);
        Assert.False(detail.Solved);
    }

    [Fact]
    public async Task JavaScript_AllTestsPassed_IsVerifiedSolve_AndFirstSolveOnlyOnce()
    {
        var (db, userId, problemId, total) = await SetupAsync();
        using var dbScope = db;
        var service = TestSupport.Problems(db);
        var dto = new SubmitSolutionDto { Language = "javascript", Code = "function twoSum(){}", PassedTests = total, TotalTests = total };

        var first = await service.SubmitAsync(userId, problemId, dto);
        var second = await service.SubmitAsync(userId, problemId, dto);

        Assert.True(first.Passed);
        Assert.True(first.Verified);
        Assert.True(first.FirstSolve);
        Assert.False(second.FirstSolve);
        Assert.True((await service.GetAsync(userId, "two-sum")).Solved);
    }

    [Fact]
    public async Task JavaScript_SomeTestsFailed_IsNotASolve()
    {
        var (db, userId, problemId, total) = await SetupAsync();
        using var dbScope = db;
        var service = TestSupport.Problems(db);

        var result = await service.SubmitAsync(userId, problemId,
            new SubmitSolutionDto { Language = "javascript", Code = "x", PassedTests = total - 1, TotalTests = total });

        Assert.False(result.Passed);
        Assert.False((await service.GetAsync(userId, "two-sum")).Solved);
    }

    [Theory]
    [InlineData(99, 99)]  // total does not match the problem
    [InlineData(-1, 6)]   // negative passed count
    [InlineData(7, 6)]    // more passed than total
    public async Task JavaScript_InconsistentReport_IsRejected(int passed, int reportedTotal)
    {
        var (db, userId, problemId, _) = await SetupAsync();
        using var dbScope = db;

        await Assert.ThrowsAsync<BusinessRuleException>(() => TestSupport.Problems(db).SubmitAsync(userId, problemId,
            new SubmitSolutionDto { Language = "javascript", Code = "x", PassedTests = passed, TotalTests = reportedTotal }));
    }

    [Fact]
    public async Task OtherLanguages_AreOnlySolvedWhenSelfReported_AndNeverVerified()
    {
        var (db, userId, problemId, _) = await SetupAsync();
        using var dbScope = db;
        var service = TestSupport.Problems(db);

        var notReported = await service.SubmitAsync(userId, problemId,
            new SubmitSolutionDto { Language = "python", Code = "def f(): pass" });
        var reported = await service.SubmitAsync(userId, problemId,
            new SubmitSolutionDto { Language = "python", Code = "def f(): pass", SelfReported = true });

        Assert.False(notReported.Passed);
        Assert.True(reported.Passed);
        Assert.False(reported.Verified);
    }

    [Fact]
    public async Task UnsupportedLanguage_IsRejected()
    {
        var (db, userId, problemId, _) = await SetupAsync();
        using var dbScope = db;

        await Assert.ThrowsAsync<BusinessRuleException>(() => TestSupport.Problems(db).SubmitAsync(userId, problemId,
            new SubmitSolutionDto { Language = "brainfuck", Code = "+" }));
    }

    [Fact]
    public async Task List_FiltersByDifficultyCompanyAndSearch_AndFlagsSolved()
    {
        var (db, userId, problemId, total) = await SetupAsync();
        using var dbScope = db;
        var service = TestSupport.Problems(db);
        await service.SubmitAsync(userId, problemId,
            new SubmitSolutionDto { Language = "javascript", Code = "x", PassedTests = total, TotalTests = total });

        var hard = await service.ListAsync(userId, new ProblemFilter("hard", null, null, null));
        var aon = await service.ListAsync(userId, new ProblemFilter(null, null, "Aon", null));
        var search = await service.ListAsync(userId, new ProblemFilter(null, null, null, "two sum"));
        var all = await service.ListAsync(null, new ProblemFilter(null, null, null, null));

        Assert.Single(hard);
        Assert.Equal("Trapping Rain Water", hard[0].Title);
        Assert.NotEmpty(aon);
        Assert.Single(search);
        Assert.True(search[0].Solved);
        Assert.Equal(15, all.Count);
        Assert.DoesNotContain(all, p => p.Solved); // anonymous callers never see solved flags
        Assert.Equal("Easy", all[0].Difficulty);   // easy problems come first
    }

    [Fact]
    public async Task UnknownProblem_ThrowsNotFound()
    {
        var (db, userId, _, _) = await SetupAsync();
        using var dbScope = db;

        await Assert.ThrowsAsync<NotFoundException>(() => TestSupport.Problems(db).GetAsync(userId, "nope"));
    }
}

public class AiPaperServiceTests
{
    private static GeneratePaperDto Request(string? company = "tcs", string? topic = null, int count = 10) =>
        new() { CompanySlug = company, Topic = topic, Difficulty = "medium", NumberOfQuestions = count };

    [Fact]
    public async Task CompanyPaper_IsSavedAndLinkedToTheCompany()
    {
        using var db = await TestSupport.SeededDbAsync();
        var ai = new FakeAiClient(TestSupport.PaperJson(10));

        var result = await TestSupport.Paper(db, ai).GenerateAsync(1, Request());

        Assert.True(result.IsAiGenerated);
        Assert.Equal("TCS", result.CompanyName);
        Assert.Equal(10, result.QuestionCount);
        Assert.Contains("TCS", result.Title);
        Assert.Equal(12, result.DurationMinutes); // 10 questions x 1.2 minutes
        Assert.True(ai.LastExpectJson);
        Assert.Contains("TCS", ai.LastUserPrompt);
        Assert.Equal(19, await db.MockTests.CountAsync()); // 18 seeded + 1 generated
    }

    [Fact]
    public async Task TopicQuiz_WithoutCompany_Works()
    {
        using var db = await TestSupport.SeededDbAsync();
        var ai = new FakeAiClient(TestSupport.PaperJson(6, "SQL"));

        var result = await TestSupport.Paper(db, ai).GenerateAsync(1, Request(company: null, topic: "SQL joins", count: 6));

        Assert.Null(result.CompanyName);
        Assert.Contains("SQL joins", result.Title);
    }

    [Fact]
    public async Task InvalidAiQuestions_AreDropped_ButEnoughValidOnesAreKept()
    {
        using var db = await TestSupport.SeededDbAsync();
        // 6 valid questions plus one with two correct options and one with a single option.
        var questions = Enumerable.Range(1, 6).Select(i => (object)new
        {
            text = $"Valid {i}?",
            topic = "SQL",
            options = new[] { new { text = "A", isCorrect = false }, new { text = "B", isCorrect = true } }
        }).ToList();
        questions.Add(new { text = "Two correct?", options = new[] { new { text = "a", isCorrect = true }, new { text = "b", isCorrect = true } } });
        questions.Add(new { text = "One option", options = new[] { new { text = "a", isCorrect = true } } });
        var json = JsonSerializer.Serialize(new { title = "T", questions });

        var result = await TestSupport.Paper(db, new FakeAiClient(json)).GenerateAsync(1, Request(count: 10));

        Assert.Equal(6, result.QuestionCount);
    }

    [Fact]
    public async Task TooFewValidQuestions_ThrowsExternalServiceException_AndSavesNothing()
    {
        using var db = await TestSupport.SeededDbAsync();

        await Assert.ThrowsAsync<ExternalServiceException>(() =>
            TestSupport.Paper(db, new FakeAiClient(TestSupport.PaperJson(2))).GenerateAsync(1, Request(count: 10)));

        Assert.Equal(18, await db.MockTests.CountAsync());
    }

    [Fact]
    public async Task NeedsCompanyOrTopic()
    {
        using var db = await TestSupport.SeededDbAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            TestSupport.Paper(db, new FakeAiClient()).GenerateAsync(1, Request(company: null, topic: null)));
    }

    [Fact]
    public async Task UnknownCompany_ThrowsNotFound()
    {
        using var db = await TestSupport.SeededDbAsync();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            TestSupport.Paper(db, new FakeAiClient()).GenerateAsync(1, Request(company: "nope")));
    }

    [Fact]
    public async Task DailyLimit_StopsTheSixthGeneration()
    {
        using var db = await TestSupport.SeededDbAsync();
        var service = TestSupport.Paper(db, new FakeAiClient(TestSupport.PaperJson(5)));

        for (var i = 0; i < AiPaperService.DailyLimit; i++)
            await service.GenerateAsync(1, Request(count: 5));

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.GenerateAsync(1, Request(count: 5)));
        // Another user still has their own quota.
        await service.GenerateAsync(2, Request(count: 5));
    }
}

public class AiCoachServiceTests
{
    [Fact]
    public async Task Hint_IncludesRequestedLevel_AndNeverRequiresCode()
    {
        using var db = await TestSupport.SeededDbAsync();
        var ai = new FakeAiClient("Think about a hash map.");
        var problem = await db.Problems.FirstAsync(p => p.Slug == "two-sum");

        var reply = await TestSupport.Coach(db, ai).CoachProblemAsync(1, problem.Id,
            new CoachRequestDto { Mode = "hint", HintLevel = 2 });

        Assert.Equal("Think about a hash map.", reply.Content);
        Assert.Contains("Hint level 2 of 3", ai.LastUserPrompt);
        Assert.False(ai.LastExpectJson);
    }

    [Fact]
    public async Task Review_WithoutCode_IsRejected()
    {
        using var db = await TestSupport.SeededDbAsync();
        var problem = await db.Problems.FirstAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() => TestSupport.Coach(db, new FakeAiClient())
            .CoachProblemAsync(1, problem.Id, new CoachRequestDto { Mode = "review", Code = "  " }));
    }

    [Fact]
    public async Task Explain_IsLockedUntilTheProblemIsSolved()
    {
        using var db = await TestSupport.SeededDbAsync();
        var user = await TestSupport.AddUserAsync(db);
        var problem = await db.Problems.FirstAsync(p => p.Slug == "two-sum");
        var ai = new FakeAiClient("Use a hash map.");
        var coach = TestSupport.Coach(db, ai);
        var request = new CoachRequestDto { Mode = "explain", Code = "function twoSum(){}" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => coach.CoachProblemAsync(user.Id, problem.Id, request));
        Assert.Equal(0, ai.Calls);

        var total = (await TestSupport.Problems(db).GetAsync(user.Id, "two-sum")).TestCases.GetArrayLength();
        await TestSupport.Problems(db).SubmitAsync(user.Id, problem.Id,
            new SubmitSolutionDto { Language = "javascript", Code = "x", PassedTests = total, TotalTests = total });

        var reply = await coach.CoachProblemAsync(user.Id, problem.Id, request);
        Assert.Equal("Use a hash map.", reply.Content);
    }

    [Fact]
    public async Task Interview_FirstTurn_AsksTheFirstQuestion()
    {
        using var db = TestSupport.NewDb();
        var ai = new FakeAiClient("Tell me about yourself.");

        var reply = await TestSupport.Coach(db, ai).InterviewAsync(
            new InterviewRequestDto { Company = "Aon", Role = "Software Engineer", Type = "hr" });

        Assert.Equal("Tell me about yourself.", reply.Content);
        Assert.Contains("FIRST question", ai.LastUserPrompt);
        Assert.Contains("Aon", ai.LastUserPrompt);
    }

    [Fact]
    public async Task Interview_AfterSixQuestions_WrapsUpWithAnEvaluation()
    {
        using var db = TestSupport.NewDb();
        var ai = new FakeAiClient("Overall: 7/10.");
        var history = new List<InterviewTurnDto>();
        for (var i = 0; i < AiCoachService.InterviewQuestions; i++)
        {
            history.Add(new InterviewTurnDto { Speaker = "ai", Text = $"Question {i + 1}" });
            history.Add(new InterviewTurnDto { Speaker = "you", Text = $"Answer {i + 1}" });
        }

        await TestSupport.Coach(db, ai).InterviewAsync(
            new InterviewRequestDto { Company = "TCS", Role = "Developer", History = history });

        Assert.Contains("interview is over", ai.LastUserPrompt);
        Assert.DoesNotContain("FIRST question", ai.LastUserPrompt);
    }
}

public class DashboardServiceTests
{
    [Fact]
    public async Task NewUser_HasAnEmptyDashboard()
    {
        using var db = await TestSupport.SeededDbAsync();
        var user = await TestSupport.AddUserAsync(db, "New Person");

        var dashboard = await TestSupport.Dashboard(db).GetAsync(user.Id);

        Assert.Equal(0, dashboard.MockTestsTaken);
        Assert.Equal(0, dashboard.ProblemsSolved);
        Assert.Equal(15, dashboard.TotalProblems);
        Assert.Equal(0, dashboard.StreakDays);
        Assert.Empty(dashboard.Topics);
    }

    [Fact]
    public async Task Activity_UpdatesScoresTopicsStreakAndLeaderboard()
    {
        using var db = await TestSupport.SeededDbAsync();
        var user = await TestSupport.AddUserAsync(db, "Sakshi Rawat");
        var other = await TestSupport.AddUserAsync(db, "Idle Learner");
        var test = await TestSupport.LoadTestAsync(db, "TCS NQT Style Mock 1");
        var attempts = TestSupport.Attempts(db);

        var started = await attempts.StartAsync(user.Id, new Application.Dtos.StartAttemptDto { MockTestId = test.Id });
        await attempts.SubmitAsync(user.Id, started.AttemptId, TestSupport.AnswerAll(test, correct: false));

        var problem = await db.Problems.FirstAsync(p => p.Slug == "two-sum");
        var total = (await TestSupport.Problems(db).GetAsync(user.Id, "two-sum")).TestCases.GetArrayLength();
        await TestSupport.Problems(db).SubmitAsync(user.Id, problem.Id,
            new Application.Dtos.SubmitSolutionDto { Language = "javascript", Code = "x", PassedTests = total, TotalTests = total });

        var service = TestSupport.Dashboard(db);
        var dashboard = await service.GetAsync(user.Id);
        var board = await service.GetLeaderboardAsync(user.Id);

        Assert.Equal(1, dashboard.MockTestsTaken);
        Assert.Equal(0, dashboard.AveragePercent);
        Assert.Equal(1, dashboard.ProblemsSolved);
        Assert.Equal(1, dashboard.SolvedByDifficulty.Easy);
        Assert.Equal(1, dashboard.StreakDays);
        Assert.NotEmpty(dashboard.WeakTopics);                       // all answers were wrong
        Assert.All(dashboard.Topics, t => Assert.Equal(0, t.Percent));

        Assert.Single(board);                                        // the idle user has no points
        Assert.Equal("Sakshi R.", board[0].Name);
        Assert.Equal(15, board[0].Points);                           // 1 solved x 10 + 1 test x 5
        Assert.True(board[0].IsYou);
        Assert.DoesNotContain(board, e => e.Name.Contains("Idle"));
        Assert.NotEqual(user.Id, other.Id);
    }
}
