using System.Text.Json;
using PrepPilot.Application.Abstractions;
using PrepPilot.Application.Dtos;
using PrepPilot.Application.Exceptions;
using PrepPilot.Domain.Entities;

namespace PrepPilot.Application.Services;

public record ProblemFilter(string? Difficulty, string? Topic, string? Company, string? Search);

public interface IProblemService
{
    Task<List<ProblemSummaryDto>> ListAsync(int? userId, ProblemFilter filter, CancellationToken ct = default);
    Task<ProblemDetailDto> GetAsync(int? userId, string slug, CancellationToken ct = default);
    Task<SubmissionResultDto> SubmitAsync(int userId, int problemId, SubmitSolutionDto dto, CancellationToken ct = default);
    Task<List<SubmissionDto>> GetSubmissionsAsync(int userId, int problemId, CancellationToken ct = default);
}

public class ProblemService(IProblemRepository problems, IUnitOfWork unitOfWork, TimeProvider time) : IProblemService
{
    private static readonly string[] Languages = ["javascript", "python", "java", "cpp"];

    public async Task<List<ProblemSummaryDto>> ListAsync(int? userId, ProblemFilter filter, CancellationToken ct = default)
    {
        var all = await problems.ListAsync(ct);
        var solved = userId is null ? new HashSet<int>() : await problems.GetSolvedProblemIdsAsync(userId.Value, ct);

        IEnumerable<Problem> query = all;

        if (!string.IsNullOrWhiteSpace(filter.Difficulty))
            query = query.Where(p => p.Difficulty.Equals(filter.Difficulty.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter.Topic))
            query = query.Where(p => Mapper.SplitCsv(p.Topics).Contains(filter.Topic.Trim(), StringComparer.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter.Company))
            query = query.Where(p => Mapper.SplitCsv(p.Companies).Contains(filter.Company.Trim(), StringComparer.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(p => p.Title.Contains(filter.Search.Trim(), StringComparison.OrdinalIgnoreCase));

        return query
            .OrderBy(p => DifficultyRank(p.Difficulty)).ThenBy(p => p.Title)
            .Select(p => new ProblemSummaryDto(
                p.Id, p.Title, p.Slug, p.Difficulty, Mapper.SplitCsv(p.Topics), Mapper.SplitCsv(p.Companies), solved.Contains(p.Id)))
            .ToList();
    }

    public async Task<ProblemDetailDto> GetAsync(int? userId, string slug, CancellationToken ct = default)
    {
        var p = await problems.GetBySlugAsync(slug.Trim().ToLowerInvariant(), ct)
                ?? throw new NotFoundException($"Problem '{slug}' was not found.");

        var solved = userId is not null && (await problems.GetSolvedProblemIdsAsync(userId.Value, ct)).Contains(p.Id);

        using var doc = JsonDocument.Parse(p.TestCasesJson);
        return new ProblemDetailDto(
            p.Id, p.Title, p.Slug, p.Difficulty, Mapper.SplitCsv(p.Topics), Mapper.SplitCsv(p.Companies),
            p.Description, p.Constraints, p.Examples, p.FunctionName, p.StarterCode,
            doc.RootElement.Clone(), solved);
    }

    public async Task<SubmissionResultDto> SubmitAsync(int userId, int problemId, SubmitSolutionDto dto, CancellationToken ct = default)
    {
        var problem = await problems.GetByIdAsync(problemId, ct)
                      ?? throw new NotFoundException($"Problem {problemId} was not found.");

        var language = dto.Language.Trim().ToLowerInvariant();
        if (!Languages.Contains(language))
            throw new BusinessRuleException($"Language must be one of: {string.Join(", ", Languages)}.");

        var actualTests = CountTests(problem);
        bool passed, verified;
        int passedTests;

        if (language == "javascript")
        {
            // JavaScript is executed in the student's browser. The server can only check that the report is consistent.
            if (dto.TotalTests != actualTests || dto.PassedTests < 0 || dto.PassedTests > actualTests)
                throw new BusinessRuleException("The test report does not match this problem. Run the tests again.");

            passedTests = dto.PassedTests;
            passed = actualTests > 0 && passedTests == actualTests;
            verified = passed;
        }
        else
        {
            passed = dto.SelfReported;
            verified = false;
            passedTests = passed ? actualTests : 0;
        }

        var alreadySolved = (await problems.GetSolvedProblemIdsAsync(userId, ct)).Contains(problem.Id);

        await problems.AddSubmissionAsync(new ProblemSubmission
        {
            ProblemId = problem.Id,
            UserId = userId,
            Language = language,
            Code = dto.Code,
            Passed = passed,
            Verified = verified,
            PassedTests = passedTests,
            TotalTests = actualTests,
            SubmittedAtUtc = time.GetUtcNow().UtcDateTime
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new SubmissionResultDto(passed, verified, passed && !alreadySolved);
    }

    public async Task<List<SubmissionDto>> GetSubmissionsAsync(int userId, int problemId, CancellationToken ct = default)
    {
        var list = await problems.GetSubmissionsAsync(problemId, userId, 5, ct);
        return list.Select(s => new SubmissionDto(
            s.Id, s.Language, s.Passed, s.Verified, s.PassedTests, s.TotalTests, s.SubmittedAtUtc, s.Code)).ToList();
    }

    private static int CountTests(Problem p)
    {
        using var doc = JsonDocument.Parse(p.TestCasesJson);
        return doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.GetArrayLength() : 0;
    }

    private static int DifficultyRank(string d) => d switch { "Easy" => 0, "Medium" => 1, _ => 2 };
}
