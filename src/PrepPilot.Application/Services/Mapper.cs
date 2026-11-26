using PrepPilot.Application.Dtos;
using PrepPilot.Domain.Entities;

namespace PrepPilot.Application.Services;

internal static class Mapper
{
    public static MockTestSummaryDto ToSummary(MockTest t) => new(
        t.Id, t.Title, t.Description, t.Company?.Name, t.Company?.Slug, t.Difficulty,
        t.DurationMinutes, t.Questions.Count, t.PassMarkPercent, t.IsAiGenerated);

    public static List<string> SplitCsv(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    /// <summary>"easy" -> "Easy" (difficulty is stored capitalised).</summary>
    public static string NormalizeDifficulty(string value)
    {
        var v = value.Trim();
        return v.Length == 0 ? v : char.ToUpperInvariant(v[0]) + v[1..].ToLowerInvariant();
    }
}
