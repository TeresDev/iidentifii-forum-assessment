using Forum.Application.Contracts;
using Forum.Domain.Entities;

namespace Forum.Application.Abstractions;

public interface ICommentRepository
{
    Task<PagedResult<CommentDto>> ListByPostAsync(
        Guid postId,
        CommentListQuery query,
        CancellationToken ct);

    Task<CommentDto> AddAsync(Comment comment, CancellationToken ct);
}
