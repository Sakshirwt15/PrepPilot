using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PrepPilot.Domain.Entities;

namespace PrepPilot.Infrastructure.Persistence;

internal record SeedPaper(string Title, string Difficulty, int DurationMinutes, int PassMark, int Offset, Dictionary<string, int> Mix);

internal record SeedCompany(
    string Name, string Slug, string Emoji, string Difficulty, string Description,
    List<string> Pattern, List<SeedPaper> Papers);

internal record SeedQuestion(string Text, List<string> Options, int Answer, string Explanation);

internal record SeedProblem(
    string Title, string Slug, string Difficulty, string Topics, string Companies, string Description,
    string Constraints, string Examples, string FunctionName, string StarterCode, JsonElement TestCases);

public static class DatabaseInitializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Creates the schema (if missing) and optionally seeds companies, mock papers and DSA problems.
    /// Retries because SQL Server in Docker can take a while to accept connections.
    /// </summary>
    public static async Task InitializeAsync(AppDbContext db, bool seed, CancellationToken ct = default)
    {
        const int maxAttempts = 10;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync(ct);
                break;
            }
            catch (Exception) when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }

        if (!seed) return;

        if (!await db.Companies.AnyAsync(ct))
        {
            var pool = Load<Dictionary<string, List<SeedQuestion>>>("questions.json");
            var companies = Load<List<SeedCompany>>("companies.json");
            foreach (var c in companies)
                db.Companies.Add(BuildCompany(c, pool));
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Problems.AnyAsync(ct))
        {
            foreach (var p in Load<List<SeedProblem>>("problems.json"))
            {
                db.Problems.Add(new Problem
                {
                    Title = p.Title,
                    Slug = p.Slug,
                    Difficulty = p.Difficulty,
                    Topics = p.Topics,
                    Companies = p.Companies,
                    Description = p.Description,
                    Constraints = p.Constraints,
                    Examples = p.Examples,
                    FunctionName = p.FunctionName,
                    StarterCode = p.StarterCode,
                    TestCasesJson = p.TestCases.GetRawText()
                });
            }
            await db.SaveChangesAsync(ct);
        }
    }

    private static Company BuildCompany(SeedCompany seed, Dictionary<string, List<SeedQuestion>> pool)
    {
        var now = DateTime.UtcNow;
        var company = new Company
        {
            Name = seed.Name,
            Slug = seed.Slug,
            Emoji = seed.Emoji,
            Difficulty = seed.Difficulty,
            Description = seed.Description,
            Pattern = string.Join("\n", seed.Pattern)
        };

        foreach (var paper in seed.Papers)
        {
            var test = new MockTest
            {
                Title = paper.Title,
                Description = $"Practice paper modelled on the typical {seed.Name} assessment pattern. Not an official or past paper.",
                Difficulty = paper.Difficulty,
                DurationMinutes = paper.DurationMinutes,
                PassMarkPercent = paper.PassMark,
                IsAiGenerated = false,
                CreatedAtUtc = now
            };

            foreach (var (topic, count) in paper.Mix)
            {
                var source = pool[topic];
                for (var i = 0; i < count; i++)
                {
                    var q = source[(paper.Offset + i) % source.Count];
                    test.Questions.Add(new Question
                    {
                        Text = q.Text,
                        Topic = topic,
                        Points = 1,
                        Explanation = q.Explanation,
                        Options = q.Options
                            .Select((text, index) => new Option { Text = text, IsCorrect = index == q.Answer })
                            .ToList()
                    });
                }
            }

            company.MockTests.Add(test);
        }

        return company;
    }

    private static T Load<T>(string resourceName)
    {
        using var stream = typeof(DatabaseInitializer).Assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
        return JsonSerializer.Deserialize<T>(stream, JsonOptions)
               ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' is empty.");
    }
}
