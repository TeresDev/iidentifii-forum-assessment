using Forum.Application.Contracts;
using Forum.Domain.Entities;

namespace Forum.Application.Abstractions;

/// <summary>
/// Methods are intention-revealing and return finished shapes. Nothing here exposes
/// IQueryable: a repository that hands out a queryable leaks the persistence technology
/// through the abstraction that exists to contain it, and the caller ends up composing
/// database queries in the application layer anyway.
/// </summary>
public interface IPostRepository
{
    Task<PagedResult<PostSummary>> ListAsync(PostListQuery query, Guid? currentUserId, CancellationToken ct);

    Task<PostDetail?> GetDetailAsync(Guid postId, Guid? currentUserId, CancellationToken ct);

    Task<Post?> FindAsync(Guid postId, CancellationToken ct);

    Task<bool> ExistsAsync(Guid postId, CancellationToken ct);

    Task AddAsync(Post post, CancellationToken ct);
}
