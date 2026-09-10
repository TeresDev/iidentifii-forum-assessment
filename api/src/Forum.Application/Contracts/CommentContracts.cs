using System.ComponentModel.DataAnnotations;

namespace Forum.Application.Contracts;

public record CommentDto(
    Guid Id,
    Guid PostId,
    Guid AuthorId,
    string AuthorUsername,
    string Body,
    DateTime CreatedAtUtc);

public record CreateCommentRequest(
    [Required][StringLength(2_000, MinimumLength = 1)] string Body);

public record CommentListQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 50)]
    public int PageSize { get; init; } = 10;
}

public record LikeCountResponse(int LikeCount);
