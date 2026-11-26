using PrepPilot.Application.Abstractions;
using PrepPilot.Application.Dtos;
using PrepPilot.Application.Exceptions;

namespace PrepPilot.Application.Services;

public interface ICompanyService
{
    Task<List<CompanySummaryDto>> ListAsync(CancellationToken ct = default);
    Task<CompanyDetailDto> GetAsync(string slug, CancellationToken ct = default);
}

public class CompanyService(ICompanyRepository companies, IMockTestRepository tests) : ICompanyService
{
    public async Task<List<CompanySummaryDto>> ListAsync(CancellationToken ct = default)
    {
        var list = await companies.ListAsync(ct);
        var counts = await tests.CountByCompanyAsync(ct);

        return list
            .Select(c => new CompanySummaryDto(
                c.Id, c.Name, c.Slug, c.Emoji, c.Description, c.Difficulty,
                counts.TryGetValue(c.Id, out var n) ? n : 0))
            .ToList();
    }

    public async Task<CompanyDetailDto> GetAsync(string slug, CancellationToken ct = default)
    {
        var company = await companies.GetBySlugAsync(slug.Trim().ToLowerInvariant(), ct)
                      ?? throw new NotFoundException($"Company '{slug}' was not found.");

        var papers = await tests.ListAsync(company.Id, null, ct);

        return new CompanyDetailDto(
            company.Id, company.Name, company.Slug, company.Emoji, company.Description, company.Difficulty,
            company.Pattern.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            papers.Select(Mapper.ToSummary).ToList());
    }
}

public interface IMockTestService
{
    Task<List<MockTestSummaryDto>> ListAsync(string? companySlug, string? difficulty, CancellationToken ct = default);
    Task<MockTestDetailDto> GetAsync(int id, CancellationToken ct = default);
}

public class MockTestService(IMockTestRepository tests, ICompanyRepository companies) : IMockTestService
{
    public async Task<List<MockTestSummaryDto>> ListAsync(string? companySlug, string? difficulty, CancellationToken ct = default)
    {
        int? companyId = null;
        if (!string.IsNullOrWhiteSpace(companySlug))
        {
            var company = await companies.GetBySlugAsync(companySlug.Trim().ToLowerInvariant(), ct)
                          ?? throw new NotFoundException($"Company '{companySlug}' was not found.");
            companyId = company.Id;
        }

        var normalized = string.IsNullOrWhiteSpace(difficulty) ? null : Mapper.NormalizeDifficulty(difficulty);
        var list = await tests.ListAsync(companyId, normalized, ct);
        return list.Select(Mapper.ToSummary).ToList();
    }

    /// <summary>Test-taking view: never includes which option is correct.</summary>
    public async Task<MockTestDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var t = await tests.GetWithQuestionsAsync(id, ct)
                ?? throw new NotFoundException($"Mock test {id} was not found.");

        return new MockTestDetailDto(
            t.Id, t.Title, t.Description, t.Company?.Name, t.Difficulty, t.DurationMinutes, t.PassMarkPercent,
            t.Questions.OrderBy(q => q.Id).Select(q => new QuestionDto(
                q.Id, q.Text, q.Topic, q.Points,
                q.Options.OrderBy(o => o.Id).Select(o => new OptionDto(o.Id, o.Text)).ToList())).ToList());
    }
}
