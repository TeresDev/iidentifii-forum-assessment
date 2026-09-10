using Forum.Api.Auth;
using Forum.Application.Contracts;
using Forum.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Controllers;

[ApiController]
// One named policy on the controller rather than role checks inside each action. This is
// the enforcement point: the Angular client hides the tag control for non-moderators, but
// that is presentation only — a third-party consumer calling this endpoint directly with a
// valid regular-user token is refused here.
[Authorize(Policy = AuthPolicies.Moderator)]
[Route("api/v1/posts/{postId:guid}/tags")]
public class PostTagsController(TagService tags) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<TagDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TagDto>> Apply(
        Guid postId,
        AddTagRequest request,
        CancellationToken ct)
    {
        var tag = await tags.ApplyAsync(postId, request.Slug, User.UserId(), ct);

        return Created($"/api/v1/posts/{postId}/tags/{tag.Slug}", tag);
    }

    [HttpDelete("{slug}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid postId, string slug, CancellationToken ct)
    {
        await tags.RemoveAsync(postId, slug, ct);

        return NoContent();
    }
}
