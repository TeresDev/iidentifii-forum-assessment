using Forum.Api.Auth;
using Forum.Application.Contracts;
using Forum.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Controllers;

[ApiController]
[Route("api/v1/posts")]
public class PostsController(PostService posts) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedResult<PostSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<PostSummary>>> List(
        [FromQuery] PostListQuery query,
        CancellationToken ct) =>
        await posts.ListAsync(query, User.UserIdOrNull(), ct);

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<PostDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDetail>> Get(Guid id, CancellationToken ct) =>
        await posts.GetAsync(id, User.UserIdOrNull(), ct);

    [HttpPost]
    [Authorize]
    [ProducesResponseType<PostDetail>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PostDetail>> Create(CreatePostRequest request, CancellationToken ct)
    {
        var post = await posts.CreateAsync(request, User.UserId(), ct);

        return CreatedAtAction(nameof(Get), new { id = post.Id }, post);
    }
}
