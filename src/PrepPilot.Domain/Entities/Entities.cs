namespace PrepPilot.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Emoji { get; set; } = "🏢";
    public string Description { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Medium";

    /// <summary>Typical hiring rounds, one per line.</summary>
    public string Pattern { get; set; } = string.Empty;

    public List<MockTest> MockTests { get; set; } = new();
}

public class MockTest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Difficulty { get; set; } = "Medium";
    public int DurationMinutes { get; set; } = 30;
    public int PassMarkPercent { get; set; } = 50;
    public bool IsAiGenerated { get; set; }

    /// <summary>Plain value (no FK) so deleting a user never cascades into shared content.</summary>
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public List<Question> Questions { get; set; } = new();
    public List<Attempt> Attempts { get; set; } = new();
}

public class Question
{
    public int Id { get; set; }
    public int MockTestId { get; set; }
    public MockTest? MockTest { get; set; }

    public string Text { get; set; } = string.Empty;
    public string Topic { get; set; } = "General";
    public int Points { get; set; } = 1;
    public string Explanation { get; set; } = string.Empty;

    public List<Option> Options { get; set; } = new();
}

public class Option
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public Question? Question { get; set; }

    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class Attempt
{
    public int Id { get; set; }

    public int MockTestId { get; set; }
    public MockTest? MockTest { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime StartedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }

    public int Score { get; set; }
    public int MaxScore { get; set; }
    public bool? Passed { get; set; }

    public List<AttemptAnswer> Answers { get; set; } = new();
}

/// <summary>Snapshot of a chosen option. Stored as plain ids (no FKs) to keep history intact.</summary>
public class AttemptAnswer
{
    public int Id { get; set; }
    public int AttemptId { get; set; }
    public Attempt? Attempt { get; set; }

    public int QuestionId { get; set; }
    public int SelectedOptionId { get; set; }
    public bool IsCorrect { get; set; }
}

public class Problem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Easy";

    /// <summary>Comma-separated, e.g. "Arrays,Hash Table".</summary>
    public string Topics { get; set; } = string.Empty;

    /// <summary>Comma-separated company names the problem is commonly reported for.</summary>
    public string Companies { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public string Constraints { get; set; } = string.Empty;
    public string Examples { get; set; } = string.Empty;

    public string FunctionName { get; set; } = string.Empty;
    public string StarterCode { get; set; } = string.Empty;

    /// <summary>JSON array of { "input": [args], "expected": value }.</summary>
    public string TestCasesJson { get; set; } = "[]";
}

public class ProblemSubmission
{
    public int Id { get; set; }

    public int ProblemId { get; set; }
    public Problem? Problem { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public string Language { get; set; } = "javascript";
    public string Code { get; set; } = string.Empty;
    public bool Passed { get; set; }

    /// <summary>True when all tests were executed and passed by the JavaScript runner (not self-reported).</summary>
    public bool Verified { get; set; }

    public int PassedTests { get; set; }
    public int TotalTests { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
}
