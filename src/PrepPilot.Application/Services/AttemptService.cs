using PrepPilot.Application.Abstractions;
using PrepPilot.Application.Dtos;
using PrepPilot.Application.Exceptions;
using PrepPilot.Domain.Entities;

namespace PrepPilot.Application.Services;

public interface IAttemptService
{
    Task<AttemptStartedDto> StartAsync(int userId, StartAttemptDto dto, CancellationToken ct = default);
    Task<AttemptResultDto> SubmitAsync(int userId, int attemptId, SubmitAttemptDto dto, CancellationToken ct = default);
    Task<AttemptResultDto> GetResultAsync(int userId, int attemptId, CancellationToken ct = default);
    Task<List<AttemptHistoryDto>> GetHistoryAsync(int userId, CancellationToken ct = default);
}

public class AttemptService(
    IMockTestRepository tests,
    IAttemptRepository attempts,
    IUnitOfWork unitOfWork,
    TimeProvider time) : IAttemptService
{
    public async Task<AttemptStartedDto> StartAsync(int userId, StartAttemptDto dto, CancellationToken ct = default)
    {
        var test = await tests.GetWithQuestionsAsync(dto.MockTestId, ct)
                   ?? throw new NotFoundException($"Mock test {dto.MockTestId} was not found.");

        var attempt = new Attempt
        {
            MockTestId = test.Id,
            UserId = userId,
            StartedAtUtc = time.GetUtcNow().UtcDateTime
        };

        await attempts.AddAsync(attempt, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return new AttemptStartedDto(attempt.Id, test.Id, attempt.StartedAtUtc, test.DurationMinutes);
    }

    public async Task<AttemptResultDto> SubmitAsync(int userId, int attemptId, SubmitAttemptDto dto, CancellationToken ct = default)
    {
        var attempt = await GetOwnedAttemptAsync(userId, attemptId, ct);

        if (attempt.SubmittedAtUtc is not null)
            throw new ConflictException("This attempt has already been submitted.");

        var test = await tests.GetWithQuestionsAsync(attempt.MockTestId, ct)
                   ?? throw new NotFoundException($"Mock test {attempt.MockTestId} was not found.");

        // Validate every submitted answer against the test before scoring.
        var selections = new Dictionary<int, int>();
        foreach (var answer in dto.Answers)
        {
            var question = test.Questions.FirstOrDefault(q => q.Id == answer.QuestionId)
                           ?? throw new BusinessRuleException($"Question {answer.QuestionId} does not belong to this test.");

            if (question.Options.All(o => o.Id != answer.OptionId))
                throw new BusinessRuleException($"Option {answer.OptionId} is not a valid choice for question {answer.QuestionId}.");

            if (!selections.TryAdd(answer.QuestionId, answer.OptionId))
                throw new BusinessRuleException($"Question {answer.QuestionId} was answered more than once.");
        }

        var result = ScoringCalculator.Calculate(test.Questions, selections, test.PassMarkPercent);

        attempt.SubmittedAtUtc = time.GetUtcNow().UtcDateTime;
        attempt.Score = result.Score;
        attempt.MaxScore = result.MaxScore;
        attempt.Passed = result.Passed;
        foreach (var g in result.Answers)
        {
            attempt.Answers.Add(new AttemptAnswer
            {
                QuestionId = g.QuestionId,
                SelectedOptionId = g.OptionId,
                IsCorrect = g.IsCorrect
            });
        }

        await unitOfWork.SaveChangesAsync(ct);
        return BuildResult(attempt, test);
    }

    public async Task<AttemptResultDto> GetResultAsync(int userId, int attemptId, CancellationToken ct = default)
    {
        var attempt = await GetOwnedAttemptAsync(userId, attemptId, ct);

        if (attempt.SubmittedAtUtc is null)
            throw new BusinessRuleException("This attempt has not been submitted yet.");

        var test = await tests.GetWithQuestionsAsync(attempt.MockTestId, ct)
                   ?? throw new NotFoundException($"Mock test {attempt.MockTestId} was not found.");

        return BuildResult(attempt, test);
    }

    public async Task<List<AttemptHistoryDto>> GetHistoryAsync(int userId, CancellationToken ct = default)
    {
        var list = await attempts.ListSubmittedForUserAsync(userId, 50, ct);
        return list.Select(ToHistory).ToList();
    }

    internal static AttemptHistoryDto ToHistory(Attempt a) => new(
        a.Id, a.MockTestId, a.MockTest?.Title ?? "Mock test", a.MockTest?.Company?.Name,
        a.MaxScore == 0 ? 0 : Math.Round(a.Score * 100.0 / a.MaxScore, 2),
        a.Passed ?? false, a.SubmittedAtUtc ?? a.StartedAtUtc);

    /// <summary>Someone else's attempt looks exactly like a missing one (no information leak).</summary>
    private async Task<Attempt> GetOwnedAttemptAsync(int userId, int attemptId, CancellationToken ct)
    {
        var attempt = await attempts.GetAsync(attemptId, ct);
        if (attempt is null || attempt.UserId != userId)
            throw new NotFoundException($"Attempt {attemptId} was not found.");
        return attempt;
    }

    private static AttemptResultDto BuildResult(Attempt attempt, MockTest test)
    {
        var answerByQuestion = attempt.Answers.ToDictionary(a => a.QuestionId);

        var review = test.Questions.OrderBy(q => q.Id).Select(q =>
        {
            answerByQuestion.TryGetValue(q.Id, out var answer);
            return new ReviewQuestionDto(
                q.Id, q.Text, q.Topic, q.Points, q.Explanation,
                answer is not null, answer?.IsCorrect ?? false,
                q.Options.OrderBy(o => o.Id)
                    .Select(o => new ReviewOptionDto(o.Id, o.Text, o.IsCorrect, answer?.SelectedOptionId == o.Id))
                    .ToList());
        }).ToList();

        var topics = review
            .GroupBy(r => r.Topic)
            .Select(g =>
            {
                var correct = g.Count(r => r.IsCorrect);
                return new TopicBreakdownDto(g.Key, correct, g.Count(), Math.Round(correct * 100.0 / g.Count(), 1));
            })
            .OrderBy(t => t.Topic)
            .ToList();

        var percent = attempt.MaxScore == 0 ? 0 : Math.Round(attempt.Score * 100.0 / attempt.MaxScore, 2);
        return new AttemptResultDto(
            attempt.Id, test.Id, test.Title, attempt.Score, attempt.MaxScore, percent, attempt.Passed ?? false,
            attempt.SubmittedAtUtc!.Value, topics, review);
    }
}
