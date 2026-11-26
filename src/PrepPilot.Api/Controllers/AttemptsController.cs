using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrepPilot.Application.Dtos;
using PrepPilot.Application.Services;

namespace PrepPilot.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/attempts")]
public class AttemptsController(IAttemptService service) : ControllerBase
{
    /// <summary>Starts a timed attempt of a mock test.</summary>
    [HttpPost]
    public async Task<ActionResult<AttemptStartedDto>> Start(StartAttemptDto dto, CancellationToken ct) =>
        Ok(await service.StartAsync(User.GetUserId(), dto, ct));

    /// <summary>Submits answers and returns the graded result with full review.</summary>
    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<AttemptResultDto>> Submit(int id, SubmitAttemptDto dto, CancellationToken ct) =>
        Ok(await service.SubmitAsync(User.GetUserId(), id, dto, ct));

    /// <summary>Result and review of one of your submitted attempts.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AttemptResultDto>> Get(int id, CancellationToken ct) =>
        Ok(await service.GetResultAsync(User.GetUserId(), id, ct));

    /// <summary>Your submitted attempts, newest first.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<AttemptHistoryDto>>> Mine(CancellationToken ct) =>
        Ok(await service.GetHistoryAsync(User.GetUserId(), ct));
}
