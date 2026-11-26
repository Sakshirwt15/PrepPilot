using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PrepPilot.Application.Dtos;
using PrepPilot.Application.Services;

namespace PrepPilot.Api.Controllers;

[ApiController]
[Route("api/problems")]
public class ProblemsController(IProblemService service, IAiCoachService coach) : ControllerBase
{
    /// <summary>DSA problems with optional filters. If you are signed in, solved problems are flagged.</summary>
    [HttpGet]
    public async Task<ActionResult<List<ProblemSummaryDto>>> List(
        [FromQuery] string? difficulty, [FromQuery] string? topic, [FromQuery] string? company,
        [FromQuery] string? search, CancellationToken ct) =>
        Ok(await service.ListAsync(User.TryGetUserId(), new ProblemFilter(difficulty, topic, company, search), ct));

    [HttpGet("{slug}")]
    public async Task<ActionResult<ProblemDetailDto>> Get(string slug, CancellationToken ct) =>
        Ok(await service.GetAsync(User.TryGetUserId(), slug, ct));

    /// <summary>Records a solution attempt (JavaScript results come from the in-browser test runner).</summary>
    [Authorize]
    [HttpPost("{id:int}/submissions")]
    public async Task<ActionResult<SubmissionResultDto>> Submit(int id, SubmitSolutionDto dto, CancellationToken ct) =>
        Ok(await service.SubmitAsync(User.GetUserId(), id, dto, ct));

    /// <summary>Your latest submissions for this problem.</summary>
    [Authorize]
    [HttpGet("{id:int}/submissions")]
    public async Task<ActionResult<List<SubmissionDto>>> Submissions(int id, CancellationToken ct) =>
        Ok(await service.GetSubmissionsAsync(User.GetUserId(), id, ct));

    /// <summary>AI mentor: progressive hints, code review, or (after solving) a full explanation.</summary>
    [Authorize]
    [EnableRateLimiting("ai")]
    [HttpPost("{id:int}/coach")]
    public async Task<ActionResult<CoachReplyDto>> Coach(int id, CoachRequestDto dto, CancellationToken ct) =>
        Ok(await coach.CoachProblemAsync(User.GetUserId(), id, dto, ct));
}
