using PrepPilot.Application.Services;
using PrepPilot.Domain.Entities;

namespace PrepPilot.Tests;

public class ScoringCalculatorTests
{
    // Q1 (id 1, 2 pts): option 10 wrong, 11 correct.  Q2 (id 2, 3 pts): option 20 correct, 21 wrong.
    private static List<Question> Questions() => new()
    {
        new Question
        {
            Id = 1, Points = 2,
            Options = new List<Option> { new() { Id = 10 }, new() { Id = 11, IsCorrect = true } }
        },
        new Question
        {
            Id = 2, Points = 3,
            Options = new List<Option> { new() { Id = 20, IsCorrect = true }, new() { Id = 21 } }
        }
    };

    [Fact]
    public void AllCorrect_GivesFullScoreAndPasses()
    {
        var result = ScoringCalculator.Calculate(Questions(), new Dictionary<int, int> { [1] = 11, [2] = 20 }, 60);

        Assert.Equal(5, result.Score);
        Assert.Equal(5, result.MaxScore);
        Assert.Equal(100, result.Percent);
        Assert.True(result.Passed);
    }

    [Fact]
    public void AllWrong_GivesZeroAndFails()
    {
        var result = ScoringCalculator.Calculate(Questions(), new Dictionary<int, int> { [1] = 10, [2] = 21 }, 60);

        Assert.Equal(0, result.Score);
        Assert.False(result.Passed);
        Assert.All(result.Answers, a => Assert.False(a.IsCorrect));
    }

    [Fact]
    public void Unanswered_ScoreZeroAndAreNotRecorded()
    {
        var result = ScoringCalculator.Calculate(Questions(), new Dictionary<int, int> { [2] = 20 }, 60);

        Assert.Equal(3, result.Score);
        Assert.Single(result.Answers);
    }

    [Theory]
    [InlineData(60, true)]
    [InlineData(61, false)]
    public void PassMark_IsInclusive(int passMark, bool expected)
    {
        var result = ScoringCalculator.Calculate(Questions(), new Dictionary<int, int> { [1] = 10, [2] = 20 }, passMark);

        Assert.Equal(60, result.Percent);
        Assert.Equal(expected, result.Passed);
    }

    [Fact]
    public void NoQuestions_DoesNotDivideByZero()
    {
        var result = ScoringCalculator.Calculate(new List<Question>(), new Dictionary<int, int>(), 60);
        Assert.Equal(0, result.Percent);
    }
}

public class StreakCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 11, 25);

    [Fact]
    public void NoActivity_IsZero() =>
        Assert.Equal(0, StreakCalculator.Calculate(Array.Empty<DateOnly>(), Today));

    [Fact]
    public void ConsecutiveDaysEndingToday_AreCounted() =>
        Assert.Equal(3, StreakCalculator.Calculate(new[] { Today, Today.AddDays(-1), Today.AddDays(-2) }, Today));

    [Fact]
    public void StreakThatEndedYesterday_StillCounts() =>
        Assert.Equal(2, StreakCalculator.Calculate(new[] { Today.AddDays(-1), Today.AddDays(-2) }, Today));

    [Fact]
    public void MissedDay_ResetsTheStreak() =>
        Assert.Equal(0, StreakCalculator.Calculate(new[] { Today.AddDays(-2), Today.AddDays(-3) }, Today));

    [Fact]
    public void GapInTheMiddle_OnlyCountsTheLatestRun() =>
        Assert.Equal(2, StreakCalculator.Calculate(new[] { Today, Today.AddDays(-1), Today.AddDays(-3) }, Today));

    [Fact]
    public void DuplicateDays_AreCountedOnce() =>
        Assert.Equal(1, StreakCalculator.Calculate(new[] { Today, Today, Today }, Today));
}

public class MiscLogicTests
{
    [Theory]
    [InlineData("Sakshi Rawat", "Sakshi R.")]
    [InlineData("Sakshi Kumari Rawat", "Sakshi R.")]
    [InlineData("Madonna", "Madonna")]
    public void MaskName_HidesSurnames(string input, string expected) =>
        Assert.Equal(expected, DashboardService.MaskName(input));

    [Fact]
    public void ParsePaper_ToleratesCodeFencesAndExtraText()
    {
        const string text = """
            Sure! Here is the paper:
            ```json
            {"title":"T","questions":[{"text":"Q?","topic":"SQL","options":[{"text":"a","isCorrect":true},{"text":"b","isCorrect":false}]}]}
            ```
            """;

        var paper = PaperPrompts.Parse(text);

        Assert.Equal("T", paper.Title);
        Assert.Single(paper.Questions);
        Assert.True(paper.Questions[0].Options[0].IsCorrect);
    }

    [Theory]
    [InlineData("no json here")]
    [InlineData("{ this is not valid json }")]
    public void ParsePaper_ThrowsOnGarbage(string text) =>
        Assert.Throws<PrepPilot.Application.Exceptions.ExternalServiceException>(() => PaperPrompts.Parse(text));
}
