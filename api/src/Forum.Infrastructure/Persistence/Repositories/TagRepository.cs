using Forum.Application.Abstractions;
using Forum.Application.Contracts;
using Forum.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forum.Infrastructure.Persistence.Repositories;

public class TagRepository(ForumDbContext db) : ITagRepository
{
    public async Task<IReadOnlyList<TagDto>> ListAsync(CancellationToken ct) =>
        await db.Tags
            .AsNoTracking()
            .OrderBy(t => t.DisplayName)
            .Select(t => new TagDto(t.Slug, t.DisplayName))
            .ToListAsync(ct);

    public Task<bool> ExistsAsync(string slug, CancellationToken ct) =>
        db.Tags.AnyAsync(t => t.Slug == slug, ct);

    public Task<TagDto?> FindAsync(string slug, CancellationToken ct) =>
        db.Tags
            .AsNoTracking()
            .Where(t => t.Slug == slug)
            .Select(t => new TagDto(t.Slug, t.DisplayName))
            .SingleOrDefaultAsync(ct);

    public Task<bool> IsAppliedAsync(Guid postId, string slug, CancellationToken ct) =>
        db.PostTags.AnyAsync(pt => pt.PostId == postId && pt.TagSlug == slug, ct);

    public async Task ApplyAsync(PostTag postTag, CancellationToken ct)
    {
        db.PostTags.Add(postTag);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(Guid postId, string slug, CancellationToken ct) =>
        await db.PostTags
            .Where(pt => pt.PostId == postId && pt.TagSlug == slug)
            .ExecuteDeleteAsync(ct);
}
