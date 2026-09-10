using Forum.Application.Abstractions;
using Forum.Application.Contracts;
using Forum.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forum.Infrastructure.Persistence.Repositories;

public class CommentRepository(ForumDbContext db) : ICommentRepository
{
    public async Task<PagedResult<CommentDto>> ListByPostAsync(
        Guid postId,
        CommentListQuery query,
        CancellationToken ct)
    {
        var comments = db.Comments.AsNoTracking().Where(c => c.PostId == postId);

        var totalCount = await comments.CountAsync(ct);

        var items = await comments
            // Oldest first, since a thread reads chronologically. The trailing Id both
            // makes the order total and matches IX_Comments_PostId_CreatedAtUtc_Id, so
            // the index satisfies the sort rather than the engine adding a sort step.
            .OrderBy(c => c.CreatedAtUtc)
            .ThenBy(c => c.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new CommentDto(
                c.Id,
                c.PostId,
                c.AuthorId,
                c.Author!.Username,
                c.Body,
                c.CreatedAtUtc))
            .ToListAsync(ct);

        return new PagedResult<CommentDto>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<CommentDto> AddAsync(Comment comment, CancellationToken ct)
    {
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);

        return await db.Comments
            .AsNoTracking()
            .Where(c => c.Id == comment.Id)
            .Select(c => new CommentDto(
                c.Id, c.PostId, c.AuthorId, c.Author!.Username, c.Body, c.CreatedAtUtc))
            .SingleAsync(ct);
    }
}
