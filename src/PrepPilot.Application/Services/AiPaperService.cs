using PrepPilot.Application.Abstractions;
using PrepPilot.Application.Dtos;
using PrepPilot.Application.Exceptions;
using PrepPilot.Domain.Entities;

namespace PrepPilot.Application.Services;

public interface IAiPaperService
{
    Task<MockTestSummaryDto> GenerateAsync(int userId, GeneratePaperDto dto, CancellationToken ct = default);
}

public class AiPaperService(
    IAiClient ai,
    ICompanyRepository companies,
    IMockTestRepository tests,
    IUnitOfWork unitOfWork,
    TimeProvider time) : IAiPaperService
{
    public const int DailyLimit = 5;

    public async Task<MockTestSummaryDto> GenerateAsync(int userId, GeneratePaperDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.CompanySlug) && string.IsNullOrWhiteSpace(dto.Topic))
            throw new BusinessRuleException("Choose a company or enter a topic.");

        var now = time.GetUtcNow().UtcDateTime;
        if (await tests.CountAiGeneratedSinceAsync(userId, now.AddHours(-24), ct) >= DailyLimit)
            throw new BusinessRuleException($"Daily limit of {DailyLimit} AI-generated tests reached. Try again tomorrow.");

        Company? company = null;
        if (!string.IsNullOrWhiteSpace(dto.CompanySlug))
        {
            company = await companies.GetBySlugAsync(dto.CompanySlug.Trim().ToLowerInvariant(), ct)
                      ?? throw new NotFoundException($"Company '{dto.CompanySlug}' was not found.");
        }

        var difficulty = dto.Difficulty.ToLowerInvariant();
        var prompt = PaperPrompts.Build(company, dto.Topic?.Trim(), difficulty, dto.NumberOfQuestions, dto.Focus);
        var raw = await ai.CompleteAsync(PaperPrompts.SystemPrompt, prompt, expectJson: true, ct);

        var paper = PaperPrompts.Parse(raw);
        var test = BuildTest(paper, dto, company, userId, now);

        await tests.AddAsync(test, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Mapper.ToSummary(test);
    }

    /// <summary>
    /// AI output is untrusted input: clamp lengths, drop invalid questions (blank text, fewer than 2 options,
    /// not exactly one correct option), and require a usable number of questions.
    /// </summary>
    private static MockTest BuildTest(AiPaperDto paper, GeneratePaperDto dto, Company? company, int userId, DateTime now)
    {
        var fallbackTopic = string.IsNullOrWhiteSpace(dto.Topic) ? "General" : dto.Topic.Trim();

        var questions = new List<Question>();
        foreach (var q in paper.Questions.Where(q => !string.IsNullOrWhiteSpace(q.Text)))
        {
            var options = q.Options
                .Where(o => !string.IsNullOrWhiteSpace(o.Text))
                .Select(o => new Option { Text = Truncate(o.Text.Trim(), 300), IsCorrect = o.IsCorrect })
                .ToList();

            if (options.Count < 2 || options.Count(o => o.IsCorrect) != 1)
                continue;

            questions.Add(new Question
            {
                Text = Truncate(q.Text.Trim(), 1000),
                Topic = Truncate(string.IsNullOrWhiteSpace(q.Topic) ? fallbackTopic : q.Topic.Trim(), 100),
                Points = 1,
                Explanation = Truncate(q.Explanation?.Trim() ?? string.Empty, 1000),
                Options = options
            });

            if (questions.Count == dto.NumberOfQuestions) break;
        }

        var minimum = Math.Max(3, dto.NumberOfQuestions / 2);
        if (questions.Count < minimum)
            throw new ExternalServiceException("The AI returned too few valid questions. Please try again.");

        var label = company is not null ? $"{company.Name} Style Mock (AI)" : $"{dto.Topic!.Trim()} Practice (AI)";
        var difficulty = Mapper.NormalizeDifficulty(dto.Difficulty);

        return new MockTest
        {
            Title = Truncate(string.IsNullOrWhiteSpace(paper.Title) ? label : $"{label}: {paper.Title.Trim()}", 200),
            Description = Truncate(paper.Description?.Trim() ?? $"AI-generated {difficulty.ToLowerInvariant()} paper.", 1000),
            Company = company,
            CompanyId = company?.Id,
            Difficulty = difficulty,
            // 1.2 minutes per question, rounded up (integer maths avoids floating-point surprises).
            DurationMinutes = Math.Max(5, (questions.Count * 6 + 4) / 5),
            PassMarkPercent = 50,
            IsAiGenerated = true,
            CreatedByUserId = userId,
            CreatedAtUtc = now,
            Questions = questions
        };
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
