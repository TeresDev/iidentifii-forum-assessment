using Forum.Domain.Entities;

namespace Forum.Application.Abstractions;

public interface ILikeRepository
{
    /// <summary>
    /// Inserts the like and updates the post's counter in one transaction.
    /// Throws <see cref="Domain.Errors.DuplicateLikeException"/> when the composite key
    /// rejects a second like by the same user.
    /// </summary>
    /// <returns>The post's like count after the insert.</returns>
    Task<int> AddAsync(Like like, CancellationToken ct);

    /// <summary>Idempotent. Returns the post's like count afterwards.</summary>
    Task<int> RemoveAsync(Guid postId, Guid userId, CancellationToken ct);
}
