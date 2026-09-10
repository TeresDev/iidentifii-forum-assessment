using Forum.Application.Abstractions;
using Forum.Domain.Entities;
using Forum.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace Forum.Infrastructure.Persistence.Repositories;

public class LikeRepository(ForumDbContext db) : ILikeRepository
{
    private const int SqliteConstraintViolation = 19;

    public async Task<int> AddAsync(Like like, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        db.Likes.Add(like);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsConstraintViolation(ex))
        {
            // The composite key rejected a second like by this user. Reaching here rather
            // than the service's pre-check means two requests raced; the database settled
            // it, which is the only place it can be settled correctly.
            await transaction.RollbackAsync(ct);
            throw new DuplicateLikeException();
        }

        // Set-based increment rather than a tracked read-modify-write, so two concurrent
        // likes cannot both read the same starting value and write the same result.
        await db.Posts
            .Where(p => p.Id == like.PostId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.LikeCount, p => p.LikeCount + 1), ct);

        await transaction.CommitAsync(ct);

        return await CurrentCountAsync(like.PostId, ct);
    }

    public async Task<int> RemoveAsync(Guid postId, Guid userId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var removed = await db.Likes
            .Where(l => l.PostId == postId && l.UserId == userId)
            .ExecuteDeleteAsync(ct);

        if (removed > 0)
        {
            await db.Posts
                .Where(p => p.Id == postId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.LikeCount, p => p.LikeCount - removed), ct);
        }

        await transaction.CommitAsync(ct);

        return await CurrentCountAsync(postId, ct);
    }

    private Task<int> CurrentCountAsync(Guid postId, CancellationToken ct) =>
        db.Posts.Where(p => p.Id == postId).Select(p => p.LikeCount).SingleAsync(ct);

    private static bool IsConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is Microsoft.Data.Sqlite.SqliteException
        {
            SqliteErrorCode: SqliteConstraintViolation
        };
}
