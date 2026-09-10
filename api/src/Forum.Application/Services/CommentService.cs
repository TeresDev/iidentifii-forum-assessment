using Forum.Application.Abstractions;
using Forum.Application.Contracts;
using Forum.Domain.Entities;
using Forum.Domain.Errors;

namespace Forum.Application.Services;

public class CommentService(
    IPostRepository posts,
    ICommentRepository comments,
    TimeProvider clock)
{
    public async Task<PagedResult<CommentDto>> ListAsync(
        Guid postId,
        CommentListQuery query,
        CancellationToken ct)
    {
        await EnsurePostExistsAsync(postId, ct);

        return await comments.ListByPostAsync(postId, query, ct);
    }

    public async Task<CommentDto> CreateAsync(
        Guid postId,
        CreateCommentRequest request,
        Guid authorId,
        CancellationToken ct)
    {
        await EnsurePostExistsAsync(postId, ct);

        return await comments.AddAsync(
            new Comment
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                AuthorId = authorId,
                Body = request.Body,
                CreatedAtUtc = clock.GetUtcNow().UtcDateTime
            },
            ct);
    }

    private async Task EnsurePostExistsAsync(Guid postId, CancellationToken ct)
    {
        if (!await posts.ExistsAsync(postId, ct))
        {
            throw new NotFoundException("Post");
        }
    }
}
