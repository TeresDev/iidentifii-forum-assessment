using System.ComponentModel.DataAnnotations;

namespace Forum.Application.Contracts;

public enum PostSort { Date, Likes }

public enum SortDirection { Asc, Desc }

/// <summary>
/// Property names are the query-string parameter names. They match
/// docs/api-contract.md exactly — renaming one here silently breaks every client,
/// because an unrecognised parameter is ignored and the response is still 200.
/// </summary>
public record PostListQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 50)]
    public int PageSize { get; init; } = 10;

    public PostSort Sort { get; init; } = PostSort.Date;

    public SortDirection Dir { get; init; } = SortDirection.Desc;

    public Guid? Author { get; init; }

    public string? Tag { get; init; }

    /// <summary>Inclusive lower bound.</summary>
    public DateTime? From { get; init; }

    /// <summary>Exclusive upper bound, so adjacent ranges tile without overlapping.</summary>
    public DateTime? To { get; init; }
}

public record TagDto(string Slug, string DisplayName);

public record PostSummary(
    Guid Id,
    string Title,
    string Excerpt,
    Guid AuthorId,
    string AuthorUsername,
    DateTime CreatedAtUtc,
    int LikeCount,
    int CommentCount,
    IReadOnlyList<TagDto> Tags,
    bool LikedByCurrentUser);

public record PostDetail(
    Guid Id,
    string Title,
    string Body,
    Guid AuthorId,
    string AuthorUsername,
    DateTime CreatedAtUtc,
    int LikeCount,
    int CommentCount,
    IReadOnlyList<TagDto> Tags,
    bool LikedByCurrentUser);

public record CreatePostRequest(
    [Required][StringLength(200, MinimumLength = 5)] string Title,
    [Required][StringLength(10_000, MinimumLength = 1)] string Body);
