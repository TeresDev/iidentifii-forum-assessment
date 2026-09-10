using Forum.Application.Abstractions;
using Forum.Application.Contracts;
using Forum.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Controllers;

/// <summary>
/// Populates the front-end's tag and author filter controls. Anonymous, because browsing
/// is anonymous and the filters are part of browsing.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1")]
public class LookupsController(TagService tags, IUserRepository users) : ControllerBase
{
    [HttpGet("tags")]
    [ProducesResponseType<IReadOnlyList<TagDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> Tags(CancellationToken ct) =>
        Ok(await tags.ListAsync(ct));

    [HttpGet("users")]
    [ProducesResponseType<IReadOnlyList<UserSummary>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserSummary>>> Users(CancellationToken ct) =>
        // Projected to UserSummary in the repository, so the password hash has no path to
        // the wire even by accident.
        Ok(await users.ListAsync(ct));
}
