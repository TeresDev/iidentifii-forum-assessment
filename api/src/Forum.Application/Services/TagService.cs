using Forum.Application.Abstractions;
using Forum.Application.Contracts;
using Forum.Domain.Entities;
using Forum.Domain.Errors;

namespace Forum.Application.Services;

public class TagService(
    IPostRepository posts,
    ITagRepository tags,
    TimeProvider clock)
{
    public Task<IReadOnlyList<TagDto>> ListAsync(CancellationToken ct) => tags.ListAsync(ct);

    public async Task<TagDto> ApplyAsync(
        Guid postId,
        string slug,
        Guid moderatorId,
        CancellationToken ct)
    {
        if (!await posts.ExistsAsync(postId, ct))
        {
            throw new NotFoundException("Post");
        }

        // The vocabulary is closed and seeded. A moderator flags content against defined
        // categories; they do not invent new ones at the point of moderation.
        var tag = await tags.FindAsync(slug, ct) ?? throw new NotFoundException("Tag");

        if (await tags.IsAppliedAsync(postId, slug, ct))
        {
            throw new AlreadyTaggedException();
        }

        await tags.ApplyAsync(
            new PostTag
            {
                PostId = postId,
                TagSlug = slug,
                TaggedByUserId = moderatorId,
                TaggedAtUtc = clock.GetUtcNow().UtcDateTime
            },
            ct);

        return tag;
    }

    public async Task RemoveAsync(Guid postId, string slug, CancellationToken ct)
    {
        if (!await posts.ExistsAsync(postId, ct))
        {
            throw new NotFoundException("Post");
        }

        await tags.RemoveAsync(postId, slug, ct);
    }
}
