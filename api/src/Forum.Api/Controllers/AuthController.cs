using Forum.Application.Contracts;
using Forum.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<UserSummary>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserSummary>> Register(RegisterRequest request, CancellationToken ct)
    {
        var user = await auth.RegisterAsync(request, ct);

        return CreatedAtAction(nameof(Register), new { id = user.Id }, user);
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct) =>
        await auth.LoginAsync(request, ct);
}
