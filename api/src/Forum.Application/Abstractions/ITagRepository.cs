using Forum.Application.Contracts;
using Forum.Domain.Entities;

namespace Forum.Application.Abstractions;

public interface ITagRepository
{
    Task<IReadOnlyList<TagDto>> ListAsync(CancellationToken ct);

    Task<bool> ExistsAsync(string slug, CancellationToken ct);

    Task<TagDto?> FindAsync(string slug, CancellationToken ct);

    Task<bool> IsAppliedAsync(Guid postId, string slug, CancellationToken ct);

    Task ApplyAsync(PostTag postTag, CancellationToken ct);

    Task RemoveAsync(Guid postId, string slug, CancellationToken ct);
}
