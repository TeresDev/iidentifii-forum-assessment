using Forum.Application.Abstractions;
using Forum.Domain.Entities;
using Forum.Domain.Errors;

namespace Forum.Application.Services;

public class LikeService(IPostRepository posts, ILikeRepository likes, TimeProvider clock)
{
    public async Task<int> LikeAsync(Guid postId, Guid userId, CancellationToken ct)
    {
        var post = await posts.FindAsync(postId, ct) ?? throw new NotFoundException("Post");

        post.EnsureCanBeLikedBy(userId);

        return await likes.AddAsync(
            new Like { PostId = postId, UserId = userId, CreatedAtUtc = clock.GetUtcNow().UtcDateTime },
            ct);
    }

    public async Task<int> UnlikeAsync(Guid postId, Guid userId, CancellationToken ct)
    {
        if (!await posts.ExistsAsync(postId, ct))
        {
            throw new NotFoundException("Post");
        }

        return await likes.RemoveAsync(postId, userId, ct);
    }
}
