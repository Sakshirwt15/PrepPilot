using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace PrepPilot.Application.Dtos;

// ------------------------------------------------------------ Auth
public class RegisterDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public record UserDto(int Id, string FullName, string Email);

public record AuthResponseDto(string Token, DateTime ExpiresAtUtc, UserDto User);

// ------------------------------------------------------------ Companies and mock tests
public record CompanySummaryDto(
    int Id, string Name, string Slug, string Emoji, string Description, string Difficulty, int PaperCount);

public record CompanyDetailDto(
    int Id, string Name, string Slug, string Emoji, string Description, string Difficulty,
    List<string> Pattern, List<MockTestSummaryDto> Papers);

public record MockTestSummaryDto(
    int Id, string Title, string Description, string? CompanyName, string? CompanySlug, string Difficulty,
    int DurationMinutes, int QuestionCount, int PassMarkPercent, bool IsAiGenerated);

// Note: option DTOs shown BEFORE submission deliberately have no IsCorrect.
public record OptionDto(int Id, string Text);

public record QuestionDto(int Id, string Text, string Topic, int Points, List<OptionDto> Options);

public record MockTestDetailDto(
    int Id, string Title, string Description, string? CompanyName, string Difficulty,
    int DurationMinutes, int PassMarkPercent, List<QuestionDto> Questions);

// ------------------------------------------------------------ Attempts
public class StartAttemptDto
{
    [Range(1, int.MaxValue)]
    public int MockTestId { get; set; }
}

public class AnswerDto
{
    public int QuestionId { get; set; }
    public int OptionId { get; set; }
}

public class SubmitAttemptDto
{
    [Required]
    public List<AnswerDto> Answers { get; set; } = new();
}

public record AttemptStartedDto(int AttemptId, int MockTestId, DateTime StartedAtUtc, int DurationMinutes);

public record ReviewOptionDto(int Id, string Text, bool IsCorrect, bool IsSelected);

public record ReviewQuestionDto(
    int QuestionId, string Text, string Topic, int Points, string Explanation,
    bool Answered, bool IsCorrect, List<ReviewOptionDto> Options);

public record TopicBreakdownDto(string Topic, int Correct, int Total, double Percent);

public record AttemptResultDto(
    int AttemptId, int MockTestId, string Title, int Score, int MaxScore, double Percent, bool Passed,
    DateTime SubmittedAtUtc, List<TopicBreakdownDto> Topics, List<ReviewQuestionDto> Review);

public record AttemptHistoryDto(
    int AttemptId, int MockTestId, string Title, string? CompanyName, double Percent, bool Passed, DateTime SubmittedAtUtc);

// ------------------------------------------------------------ DSA problems
public record ProblemSummaryDto(
    int Id, string Title, string Slug, string Difficulty, List<string> Topics, List<string> Companies, bool Solved);

public record ProblemDetailDto(
    int Id, string Title, string Slug, string Difficulty, List<string> Topics, List<string> Companies,
    string Description, string Constraints, string Examples, string FunctionName, string StarterCode,
    JsonElement TestCases, bool Solved);

public class SubmitSolutionDto
{
    [Required, StringLength(20)]
    public string Language { get; set; } = "javascript";

    [Required, StringLength(20000)]
    public string Code { get; set; } = string.Empty;

    public int PassedTests { get; set; }
    public int TotalTests { get; set; }

    /// <summary>For languages that cannot be run in the browser: the user marks the problem solved themselves.</summary>
    public bool SelfReported { get; set; }
}

public record SubmissionDto(
    int Id, string Language, bool Passed, bool Verified, int PassedTests, int TotalTests, DateTime SubmittedAtUtc, string Code);

public record SubmissionResultDto(bool Passed, bool Verified, bool FirstSolve);

// ------------------------------------------------------------ AI
public class GeneratePaperDto
{
    [StringLength(60)]
    public string? CompanySlug { get; set; }

    [StringLength(150)]
    public string? Topic { get; set; }

    [RegularExpression("^(easy|medium|hard)$", ErrorMessage = "Difficulty must be easy, medium or hard.")]
    public string Difficulty { get; set; } = "medium";

    [Range(5, 30)]
    public int NumberOfQuestions { get; set; } = 15;

    [RegularExpression("^(mixed|aptitude|technical)$", ErrorMessage = "Focus must be mixed, aptitude or technical.")]
    public string Focus { get; set; } = "mixed";
}

public class CoachRequestDto
{
    [Required, RegularExpression("^(hint|review|explain)$", ErrorMessage = "Mode must be hint, review or explain.")]
    public string Mode { get; set; } = "hint";

    [Required, StringLength(20)]
    public string Language { get; set; } = "javascript";

    [StringLength(20000)]
    public string Code { get; set; } = string.Empty;

    [Range(1, 3)]
    public int HintLevel { get; set; } = 1;
}

public record CoachReplyDto(string Mode, string Content);

public class InterviewTurnDto
{
    [Required, RegularExpression("^(ai|you)$")]
    public string Speaker { get; set; } = "you";

    [Required, StringLength(3000)]
    public string Text { get; set; } = string.Empty;
}

public class InterviewRequestDto
{
    [Required, StringLength(100)]
    public string Company { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Role { get; set; } = "Software Engineer";

    [RegularExpression("^(hr|technical)$")]
    public string Type { get; set; } = "technical";

    [MaxLength(30)]
    public List<InterviewTurnDto> History { get; set; } = new();
}

// What the AI is asked to return (untrusted, sanitized before saving).
public class AiOptionDto
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class AiQuestionDto
{
    public string Text { get; set; } = string.Empty;
    public string? Topic { get; set; }
    public string? Explanation { get; set; }
    public int Points { get; set; } = 1;
    public List<AiOptionDto> Options { get; set; } = new();
}

public class AiPaperDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<AiQuestionDto> Questions { get; set; } = new();
}

// ------------------------------------------------------------ Dashboard
public record TopicStatDto(string Topic, int Correct, int Total, double Percent);

public record SolvedByDifficultyDto(int Easy, int Medium, int Hard, int EasyTotal, int MediumTotal, int HardTotal);

public record DashboardDto(
    string FullName, int MockTestsTaken, double AveragePercent, double BestPercent,
    int ProblemsSolved, int TotalProblems, SolvedByDifficultyDto SolvedByDifficulty, int StreakDays,
    List<TopicStatDto> Topics, List<string> WeakTopics, List<AttemptHistoryDto> Recent);

public record LeaderboardEntryDto(
    int Rank, string Name, int ProblemsSolved, int MockTestsTaken, double AveragePercent, int Points, bool IsYou);
