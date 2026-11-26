using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace PrepPilot.Tests;

/// <summary>Guards the embedded seed files: a typo in the JSON should fail a test, not production startup.</summary>
public class SeedDataTests
{
    [Fact]
    public async Task Seeder_CreatesCompaniesAndPapers()
    {
        using var db = await TestSupport.SeededDbAsync();

        Assert.Equal(9, await db.Companies.CountAsync());
        Assert.Equal(18, await db.MockTests.CountAsync());
        Assert.All(await db.MockTests.ToListAsync(), t => Assert.NotNull(t.CompanyId));
    }

    [Fact]
    public async Task EveryPaper_HasDistinctQuestions_WithExactlyOneCorrectOption()
    {
        using var db = await TestSupport.SeededDbAsync();
        var tests = await db.MockTests.Include(t => t.Questions).ThenInclude(q => q.Options).ToListAsync();

        foreach (var test in tests)
        {
            Assert.True(test.Questions.Count >= 15, test.Title);
            Assert.Equal(test.Questions.Count, test.Questions.Select(q => q.Text).Distinct().Count());
            Assert.All(test.Questions, q =>
            {
                Assert.Equal(1, q.Options.Count(o => o.IsCorrect));
                Assert.False(string.IsNullOrWhiteSpace(q.Explanation));
            });
        }
    }

    [Fact]
    public async Task Problems_HaveValidTestCases()
    {
        using var db = await TestSupport.SeededDbAsync();
        var problems = await db.Problems.ToListAsync();

        Assert.Equal(15, problems.Count);
        Assert.Equal(problems.Count, problems.Select(p => p.Slug).Distinct().Count());

        foreach (var p in problems)
        {
            using var doc = JsonDocument.Parse(p.TestCasesJson);
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            Assert.True(doc.RootElement.GetArrayLength() >= 5, p.Title);
            Assert.Contains($"function {p.FunctionName}(", p.StarterCode);
        }
    }

    [Fact]
    public async Task Seeding_IsIdempotent()
    {
        using var db = await TestSupport.SeededDbAsync();
        await PrepPilot.Infrastructure.Persistence.DatabaseInitializer.InitializeAsync(db, seed: true);

        Assert.Equal(9, await db.Companies.CountAsync());
        Assert.Equal(15, await db.Problems.CountAsync());
    }
}
