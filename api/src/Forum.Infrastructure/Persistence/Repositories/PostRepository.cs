using Forum.Application.Abstractions;
using Forum.Application.Contracts;
using Forum.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forum.Infrastructure.Persistence.Repositories;

public class PostRepository(ForumDbContext db) : IPostRepository
{
    private const int ExcerptLength = 200;

    public async Task<PagedResult<PostSummary>> ListAsync(
        PostListQuery query,
        Guid? currentUserId,
        CancellationToken ct)
    {
        var posts = Filter(db.Posts.AsNoTracking(), query);

        var totalCount = await posts.CountAsync(ct);

        var items = await Sort(posts, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new PostSummary(
                p.Id,
                p.Title,
                p.Body.Length <= ExcerptLength ? p.Body : p.Body.Substring(0, ExcerptLength),
                p.AuthorId,
                p.Author!.Username,
                p.CreatedAtUtc,
                p.LikeCount,
                p.Comments.Count(),
                p.PostTags.Select(pt => new TagDto(pt.TagSlug, pt.Tag!.DisplayName)).ToList(),
                currentUserId != null && p.Likes.Any(l => l.UserId == currentUserId)))
            .ToListAsync(ct);

        return new PagedResult<PostSummary>(items, query.Page, query.PageSize, totalCount);
    }

    public Task<PostDetail?> GetDetailAsync(Guid postId, Guid? currentUserId, CancellationToken ct) =>
        db.Posts
            .AsNoTracking()
            .Where(p => p.Id == postId)
            .Select(p => new PostDetail(
                p.Id,
                p.Title,
                p.Body,
                p.AuthorId,
                p.Author!.Username,
                p.CreatedAtUtc,
                p.LikeCount,
                p.Comments.Count(),
                p.PostTags.Select(pt => new TagDto(pt.TagSlug, pt.Tag!.DisplayName)).ToList(),
                currentUserId != null && p.Likes.Any(l => l.UserId == currentUserId)))
            .SingleOrDefaultAsync(ct);

    public Task<Post?> FindAsync(Guid postId, CancellationToken ct) =>
        db.Posts.SingleOrDefaultAsync(p => p.Id == postId, ct);

    public Task<bool> ExistsAsync(Guid postId, CancellationToken ct) =>
        db.Posts.AnyAsync(p => p.Id == postId, ct);

    public async Task AddAsync(Post post, CancellationToken ct)
    {
        db.Posts.Add(post);
        await db.SaveChangesAsync(ct);
    }

    private static IQueryable<Post> Filter(IQueryable<Post> posts, PostListQuery query)
    {
        if (query.Author is { } author)
        {
            posts = posts.Where(p => p.AuthorId == author);
        }

        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            posts = posts.Where(p => p.PostTags.Any(pt => pt.TagSlug == query.Tag));
        }

        if (query.From is { } from)
        {
            posts = posts.Where(p => p.CreatedAtUtc >= from);
        }

        if (query.To is { } to)
        {
            // Exclusive, so [a,b) and [b,c) tile without double-counting the boundary row.
            posts = posts.Where(p => p.CreatedAtUtc < to);
        }

        return posts;
    }

    /// <summary>
    /// Every branch ends with Id as the final ordering column. Ordering by a non-unique
    /// key alone leaves tied rows in engine-defined order, which is not stable between
    /// the queries that produce page 1 and page 2 — so a row can appear on both pages
    /// while another appears on neither.
    /// </summary>
    private static IQueryable<Post> Sort(IQueryable<Post> posts, PostListQuery query) =>
        (query.Sort, query.Dir) switch
        {
            (PostSort.Likes, SortDirection.Asc) =>
                posts.OrderBy(p => p.LikeCount).ThenByDescending(p => p.Id),
            (PostSort.Likes, _) =>
                posts.OrderByDescending(p => p.LikeCount).ThenByDescending(p => p.Id),
            (_, SortDirection.Asc) =>
                posts.OrderBy(p => p.CreatedAtUtc).ThenByDescending(p => p.Id),
            _ =>
                posts.OrderByDescending(p => p.CreatedAtUtc).ThenByDescending(p => p.Id)
        };
}
