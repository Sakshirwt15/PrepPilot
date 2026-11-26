using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrepPilot.Application.Dtos;
using PrepPilot.Application.Services;

namespace PrepPilot.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService service) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto, CancellationToken ct) =>
        Ok(await service.RegisterAsync(dto, ct));

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto, CancellationToken ct) =>
        Ok(await service.LoginAsync(dto, ct));

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct) =>
        Ok(await service.GetMeAsync(User.GetUserId(), ct));
}
