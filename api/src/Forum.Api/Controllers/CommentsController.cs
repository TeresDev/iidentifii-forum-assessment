using Forum.Api.Auth;
using Forum.Application.Contracts;
using Forum.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Controllers;

[ApiController]
[Route("api/v1/posts/{postId:guid}/comments")]
public class CommentsController(CommentService comments) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedResult<CommentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<CommentDto>>> List(
        Guid postId,
        [FromQuery] CommentListQuery query,
        CancellationToken ct) =>
        await comments.ListAsync(postId, query, ct);

    [HttpPost]
    [Authorize]
    [ProducesResponseType<CommentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> Create(
        Guid postId,
        CreateCommentRequest request,
        CancellationToken ct)
    {
        var comment = await comments.CreateAsync(postId, request, User.UserId(), ct);

        return CreatedAtAction(nameof(List), new { postId }, comment);
    }
}
