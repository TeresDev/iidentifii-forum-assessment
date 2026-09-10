using Forum.Application.Abstractions;
using Forum.Application.Contracts;
using Forum.Domain.Entities;
using Forum.Domain.Errors;

namespace Forum.Application.Services;

public class PostService(IPostRepository posts, TimeProvider clock)
{
    public Task<PagedResult<PostSummary>> ListAsync(
        PostListQuery query,
        Guid? currentUserId,
        CancellationToken ct) =>
        posts.ListAsync(query, currentUserId, ct);

    public async Task<PostDetail> GetAsync(Guid postId, Guid? currentUserId, CancellationToken ct) =>
        await posts.GetDetailAsync(postId, currentUserId, ct)
        ?? throw new NotFoundException("Post");

    public async Task<PostDetail> CreateAsync(
        CreatePostRequest request,
        Guid authorId,
        CancellationToken ct)
    {
        var post = new Post
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Title = request.Title,
            Body = request.Body,
            CreatedAtUtc = clock.GetUtcNow().UtcDateTime
        };

        await posts.AddAsync(post, ct);

        return await GetAsync(post.Id, authorId, ct);
    }
}
