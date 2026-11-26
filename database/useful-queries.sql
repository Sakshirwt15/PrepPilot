-- Handy reporting queries for the PrepPilot schema (SQL Server). Run them against PrepPilotDb.
USE PrepPilotDb;
GO

-- 1. Papers per company
SELECT c.Name, COUNT(t.Id) AS Papers
FROM   Companies c
LEFT JOIN MockTests t ON t.CompanyId = c.Id
GROUP BY c.Name
ORDER BY Papers DESC, c.Name;

-- 2. Average score per mock test (submitted attempts only)
SELECT t.Title,
       COUNT(a.Id) AS Attempts,
       ROUND(AVG(CASE WHEN a.MaxScore = 0 THEN 0 ELSE a.Score * 100.0 / a.MaxScore END), 1) AS AvgPercent
FROM   MockTests t
JOIN   Attempts a ON a.MockTestId = t.Id AND a.SubmittedAtUtc IS NOT NULL
GROUP BY t.Title
ORDER BY AvgPercent DESC;

-- 3. Hardest questions: lowest share of correct answers (at least 3 answers)
SELECT q.Topic, q.Text,
       COUNT(*) AS TimesAnswered,
       ROUND(100.0 * SUM(CAST(aa.IsCorrect AS INT)) / COUNT(*), 1) AS PercentCorrect
FROM   AttemptAnswers aa
JOIN   Questions q ON q.Id = aa.QuestionId
GROUP BY q.Topic, q.Text
HAVING COUNT(*) >= 3
ORDER BY PercentCorrect ASC;

-- 4. Accuracy per topic for one user
DECLARE @UserId INT = 1;
SELECT q.Topic,
       COUNT(*) AS Answered,
       ROUND(100.0 * SUM(CAST(aa.IsCorrect AS INT)) / COUNT(*), 1) AS Accuracy
FROM   AttemptAnswers aa
JOIN   Attempts a  ON a.Id = aa.AttemptId AND a.UserId = @UserId
JOIN   Questions q ON q.Id = aa.QuestionId
GROUP BY q.Topic
ORDER BY Accuracy;

-- 5. Problems solved per user (distinct problems), with leaderboard points
SELECT u.FullName,
       COUNT(DISTINCT s.ProblemId) AS Solved,
       COUNT(DISTINCT s.ProblemId) * 10 AS ProblemPoints
FROM   Users u
JOIN   ProblemSubmissions s ON s.UserId = u.Id AND s.Passed = 1
GROUP BY u.FullName
ORDER BY Solved DESC;

-- 6. Most-solved DSA problems
SELECT p.Title, p.Difficulty, COUNT(DISTINCT s.UserId) AS Solvers
FROM   Problems p
LEFT JOIN ProblemSubmissions s ON s.ProblemId = p.Id AND s.Passed = 1
GROUP BY p.Title, p.Difficulty
ORDER BY Solvers DESC, p.Title;
