using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrepPilot.Application.Abstractions;
using PrepPilot.Application.Dtos;
using PrepPilot.Infrastructure.Persistence;

namespace PrepPilot.Tests;

/// <summary>Boots the real API in memory with a seeded InMemory database and a fake AI client.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));

            // Never call the real AI from tests: the last registration wins.
            services.AddSingleton<IAiClient>(new FakeAiClient(TestSupport.PaperJson(10)));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DatabaseInitializer.InitializeAsync(db, seed: true).GetAwaiter().GetResult();
        return host;
    }
}

public class ApiIntegrationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    /// <summary>A new client for a freshly registered user, already carrying their token.</summary>
    private async Task<(HttpClient Client, AuthResponseDto Auth)> SignUpAsync(string name = "Test Learner")
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            FullName = name,
            Email = $"{Guid.NewGuid():N}@example.com",
            Password = "Passw0rd!"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, auth);
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Companies_ArePublic_AndExposePattern()
    {
        var client = factory.CreateClient();

        var list = await client.GetFromJsonAsync<List<CompanySummaryDto>>("/api/companies");
        var tcs = await client.GetFromJsonAsync<CompanyDetailDto>("/api/companies/tcs");

        Assert.Equal(9, list!.Count);
        Assert.All(list, c => Assert.True(c.PaperCount >= 2));
        Assert.Equal(2, tcs!.Papers.Count);
        Assert.NotEmpty(tcs.Pattern);

        var missing = await client.GetAsync("/api/companies/not-a-company");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task TestQuestions_NeverLeakCorrectAnswers()
    {
        var client = factory.CreateClient();
        var tests = await client.GetFromJsonAsync<List<MockTestSummaryDto>>("/api/mock-tests?company=aon");
        Assert.Equal(2, tests!.Count);

        var raw = await client.GetStringAsync($"/api/mock-tests/{tests[0].Id}");

        Assert.DoesNotContain("isCorrect", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("explanation", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Attempts_RequireLogin()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/attempts", new StartAttemptDto { MockTestId = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/ai/generate-paper", new GeneratePaperDto { Topic = "SQL" })).StatusCode);
    }

    [Fact]
    public async Task EndToEnd_TakeATest_AndReviewTheResult()
    {
        var (client, _) = await SignUpAsync();
        var tests = await client.GetFromJsonAsync<List<MockTestSummaryDto>>("/api/mock-tests?company=tcs");
        var detail = (await client.GetFromJsonAsync<MockTestDetailDto>($"/api/mock-tests/{tests![0].Id}"))!;

        var started = (await (await client.PostAsJsonAsync("/api/attempts", new StartAttemptDto { MockTestId = detail.Id }))
            .Content.ReadFromJsonAsync<AttemptStartedDto>())!;
        Assert.Equal(detail.DurationMinutes, started.DurationMinutes);

        // Answer only the first question (the first option) and submit.
        var submit = new SubmitAttemptDto
        {
            Answers = new List<AnswerDto>
            {
                new() { QuestionId = detail.Questions[0].Id, OptionId = detail.Questions[0].Options[0].Id }
            }
        };
        var submitResponse = await client.PostAsJsonAsync($"/api/attempts/{started.AttemptId}/submit", submit);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        var result = (await submitResponse.Content.ReadFromJsonAsync<AttemptResultDto>())!;
        Assert.Equal(detail.Questions.Count, result.Review.Count);
        Assert.Contains(result.Review[0].Options, o => o.IsCorrect); // review reveals answers after submitting

        // A second submit conflicts; the result and dashboard are available.
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync($"/api/attempts/{started.AttemptId}/submit", submit)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/attempts/{started.AttemptId}")).StatusCode);

        var dashboard = await client.GetFromJsonAsync<DashboardDto>("/api/me/dashboard");
        Assert.Equal(1, dashboard!.MockTestsTaken);
        var history = await client.GetFromJsonAsync<List<AttemptHistoryDto>>("/api/attempts/mine");
        Assert.Single(history!);
    }

    [Fact]
    public async Task Users_CannotReadEachOthersResults()
    {
        var (alice, _) = await SignUpAsync("Alice A");
        var (bob, _) = await SignUpAsync("Bob B");
        var tests = await alice.GetFromJsonAsync<List<MockTestSummaryDto>>("/api/mock-tests?company=tcs");

        var started = (await (await alice.PostAsJsonAsync("/api/attempts", new StartAttemptDto { MockTestId = tests![0].Id }))
            .Content.ReadFromJsonAsync<AttemptStartedDto>())!;
        await alice.PostAsJsonAsync($"/api/attempts/{started.AttemptId}/submit", new SubmitAttemptDto());

        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/attempts/{started.AttemptId}")).StatusCode);
    }

    [Fact]
    public async Task Register_Validates_AndRejectsDuplicates()
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";

        var weak = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto { FullName = "A B", Email = email, Password = "short" });
        var badEmail = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto { FullName = "A B", Email = "not-an-email", Password = "Passw0rd!" });
        var ok = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto { FullName = "A B", Email = email, Password = "Passw0rd!" });
        var duplicate = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto { FullName = "A B", Email = email, Password = "Passw0rd!" });
        var wrongLogin = await client.PostAsJsonAsync("/api/auth/login",
            new LoginDto { Email = email, Password = "WrongPassword" });

        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badEmail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongLogin.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsTheSignedInUser()
    {
        var (client, auth) = await SignUpAsync("Sakshi Rawat");

        var me = await client.GetFromJsonAsync<UserDto>("/api/auth/me");

        Assert.Equal(auth.User.Id, me!.Id);
        Assert.Equal("Sakshi Rawat", me.FullName);
    }

    [Fact]
    public async Task Problems_ListIsPublic_AndSubmissionsNeedLogin()
    {
        var anonymous = factory.CreateClient();
        var problems = await anonymous.GetFromJsonAsync<List<ProblemSummaryDto>>("/api/problems?difficulty=easy");
        Assert.NotEmpty(problems!);
        Assert.All(problems!, p => Assert.Equal("Easy", p.Difficulty));

        var detail = await anonymous.GetFromJsonAsync<ProblemDetailDto>("/api/problems/two-sum");
        var total = detail!.TestCases.GetArrayLength();

        var body = new SubmitSolutionDto { Language = "javascript", Code = "x", PassedTests = total, TotalTests = total };
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync($"/api/problems/{detail.Id}/submissions", body)).StatusCode);

        var (client, _) = await SignUpAsync();
        var solved = await client.PostAsJsonAsync($"/api/problems/{detail.Id}/submissions", body);
        Assert.Equal(HttpStatusCode.OK, solved.StatusCode);
        Assert.True((await solved.Content.ReadFromJsonAsync<SubmissionResultDto>())!.FirstSolve);

        var again = await client.GetFromJsonAsync<ProblemDetailDto>("/api/problems/two-sum");
        Assert.True(again!.Solved);
    }

    [Fact]
    public async Task AiGenerate_CreatesAPlayablePaper()
    {
        var (client, _) = await SignUpAsync();

        var response = await client.PostAsJsonAsync("/api/ai/generate-paper",
            new GeneratePaperDto { CompanySlug = "infosys", Difficulty = "hard", NumberOfQuestions = 10 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = (await response.Content.ReadFromJsonAsync<MockTestSummaryDto>())!;
        Assert.True(summary.IsAiGenerated);
        Assert.Equal("Infosys", summary.CompanyName);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/mock-tests/{summary.Id}")).StatusCode);

        var invalid = await client.PostAsJsonAsync("/api/ai/generate-paper",
            new GeneratePaperDto { Topic = "SQL", Difficulty = "impossible" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task Leaderboard_IsPublic()
    {
        var response = await factory.CreateClient().GetAsync("/api/leaderboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
