using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PrepPilot.Application.Dtos;
using PrepPilot.Application.Services;

namespace PrepPilot.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("ai")]
[Route("api/ai")]
public class AiController(IAiPaperService papers, IAiCoachService coach) : ControllerBase
{
    /// <summary>Generates a company-style mock paper or a topic quiz with AI. Can take 10-40 seconds.</summary>
    [HttpPost("generate-paper")]
    public async Task<ActionResult<MockTestSummaryDto>> GeneratePaper(GeneratePaperDto dto, CancellationToken ct) =>
        Ok(await papers.GenerateAsync(User.GetUserId(), dto, ct));

    /// <summary>One turn of an AI mock interview. Send the transcript so far; get feedback plus the next question.</summary>
    [HttpPost("interview")]
    public async Task<ActionResult<CoachReplyDto>> Interview(InterviewRequestDto dto, CancellationToken ct) =>
        Ok(await coach.InterviewAsync(dto, ct));
}

[ApiController]
[Route("api")]
public class StatsController(IDashboardService service) : ControllerBase
{
    /// <summary>Your progress: scores, solved problems, topic accuracy, weak areas and streak.</summary>
    [Authorize]
    [HttpGet("me/dashboard")]
    public async Task<ActionResult<DashboardDto>> Dashboard(CancellationToken ct) =>
        Ok(await service.GetAsync(User.GetUserId(), ct));

    /// <summary>Top 20 learners by points (10 per solved problem, 5 per completed mock test).</summary>
    [HttpGet("leaderboard")]
    public async Task<ActionResult<List<LeaderboardEntryDto>>> Leaderboard(CancellationToken ct) =>
        Ok(await service.GetLeaderboardAsync(User.TryGetUserId(), ct));
}
