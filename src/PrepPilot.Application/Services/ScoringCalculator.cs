using PrepPilot.Domain.Entities;

namespace PrepPilot.Application.Services;

public record GradedAnswer(int QuestionId, int OptionId, bool IsCorrect);

public record ScoreResult(int Score, int MaxScore, double Percent, bool Passed, IReadOnlyList<GradedAnswer> Answers);

/// <summary>Pure scoring logic with no I/O. Unanswered questions score 0.</summary>
public static class ScoringCalculator
{
    public static ScoreResult Calculate(
        IReadOnlyCollection<Question> questions,
        IReadOnlyDictionary<int, int> selections,
        int passMarkPercent)
    {
        var maxScore = questions.Sum(q => q.Points);
        var score = 0;
        var graded = new List<GradedAnswer>();

        foreach (var question in questions)
        {
            if (!selections.TryGetValue(question.Id, out var optionId))
                continue;

            var option = question.Options.FirstOrDefault(o => o.Id == optionId);
            var isCorrect = option?.IsCorrect == true;
            if (isCorrect) score += question.Points;

            graded.Add(new GradedAnswer(question.Id, optionId, isCorrect));
        }

        var percent = maxScore == 0 ? 0 : Math.Round(score * 100.0 / maxScore, 2);
        return new ScoreResult(score, maxScore, percent, percent >= passMarkPercent, graded);
    }
}
