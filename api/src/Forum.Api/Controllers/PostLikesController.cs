using Forum.Api.Auth;
using Forum.Application.Contracts;
using Forum.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/posts/{postId:guid}/likes")]
public class PostLikesController(LikeService likes) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<LikeCountResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LikeCountResponse>> Like(Guid postId, CancellationToken ct)
    {
        var likeCount = await likes.LikeAsync(postId, User.UserId(), ct);

        return Created($"/api/v1/posts/{postId}/likes", new LikeCountResponse(likeCount));
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unlike(Guid postId, CancellationToken ct)
    {
        await likes.UnlikeAsync(postId, User.UserId(), ct);

        return NoContent();
    }
}
