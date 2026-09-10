using System.Net;
using System.Net.Http.Json;
using Forum.Application.Contracts;

namespace Forum.IntegrationTests;

public class CommentTests(ForumApiFactory factory) : IClassFixture<ForumApiFactory>
{
    private async Task<PostSummary> TheMostCommentedPostAsync()
    {
        var list = await factory.CreateClient().GetFromJsonAsync<PagedResult<PostSummary>>(
            "/api/v1/posts?pageSize=50", JsonDefaults.Options);

        return list!.Items.OrderByDescending(p => p.CommentCount).First();
    }

    private async Task<PagedResult<CommentDto>> CommentsAsync(Guid postId, string query = "")
    {
        var response = await factory.CreateClient().GetAsync($"/api/v1/posts/{postId}/comments{query}");
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<PagedResult<CommentDto>>(JsonDefaults.Options))!;
    }

    [Fact]
    public async Task Reading_comments_does_not_require_authentication()
    {
        var post = await TheMostCommentedPostAsync();

        var response = await factory.CreateClient().GetAsync($"/api/v1/posts/{post.Id}/comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Comments_are_paged()
    {
        var post = await TheMostCommentedPostAsync();
        Assert.True(post.CommentCount > 10, "seed should include a post needing more than one page");

        var page = await CommentsAsync(post.Id, "?page=1&pageSize=10");

        Assert.Equal(10, page.Items.Count);
        Assert.Equal(post.CommentCount, page.TotalCount);
        Assert.True(page.TotalPages > 1);
    }

    [Fact]
    public async Task Comment_paging_is_stable_across_pages()
    {
        var post = await TheMostCommentedPostAsync();

        var all = await CommentsAsync(post.Id, "?pageSize=50");
        var seen = new List<Guid>();

        for (var page = 1; page <= all.TotalCount; page++)
        {
            var single = await CommentsAsync(post.Id, $"?page={page}&pageSize=1");
            seen.AddRange(single.Items.Select(c => c.Id));
        }

        Assert.Equal(all.TotalCount, seen.Count);
        Assert.Equal(seen.Count, seen.Distinct().Count());
        Assert.Equal(all.Items.Select(c => c.Id), seen);
    }

    [Fact]
    public async Task Comments_read_oldest_first()
    {
        var post = await TheMostCommentedPostAsync();

        var comments = await CommentsAsync(post.Id, "?pageSize=50");

        var dates = comments.Items.Select(c => c.CreatedAtUtc).ToList();
        Assert.Equal(dates.OrderBy(d => d), dates);
    }

    [Fact]
    public async Task Comments_carry_the_author_username()
    {
        var post = await TheMostCommentedPostAsync();

        var comments = await CommentsAsync(post.Id, "?pageSize=5");

        Assert.All(comments.Items, c => Assert.False(string.IsNullOrWhiteSpace(c.AuthorUsername)));
    }

    [Fact]
    public async Task Comments_on_an_unknown_post_are_a_404()
    {
        var response = await factory.CreateClient()
            .GetAsync($"/api/v1/posts/{Guid.NewGuid()}/comments");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Commenting_requires_authentication()
    {
        var post = await TheMostCommentedPostAsync();

        var response = await factory.CreateClient().PostAsJsonAsync(
            $"/api/v1/posts/{post.Id}/comments",
            new CreateCommentRequest("Anonymous thoughts"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task An_authenticated_user_can_comment()
    {
        var client = await factory.AsAsync("ben");
        var post = await TheMostCommentedPostAsync();
        var before = (await CommentsAsync(post.Id, "?pageSize=50")).TotalCount;

        var response = await client.PostAsJsonAsync(
            $"/api/v1/posts/{post.Id}/comments",
            new CreateCommentRequest("That matches what we saw as well."));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<CommentDto>(JsonDefaults.Options);
        Assert.Equal("ben", created!.AuthorUsername);
        Assert.Equal(post.Id, created.PostId);
        Assert.Equal(before + 1, (await CommentsAsync(post.Id, "?pageSize=50")).TotalCount);
    }

    [Fact]
    public async Task An_author_may_comment_on_their_own_post()
    {
        var client = await factory.AsAsync("alice");
        var list = await client.GetFromJsonAsync<PagedResult<PostSummary>>(
            "/api/v1/posts?pageSize=50", JsonDefaults.Options);
        var ownPost = list!.Items.First(p => p.AuthorUsername == "alice");

        // Unlike liking, there is no rule against commenting on your own post — a thread
        // author answering a question is normal forum behaviour.
        var response = await client.PostAsJsonAsync(
            $"/api/v1/posts/{ownPost.Id}/comments",
            new CreateCommentRequest("Following up on my own question."));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task An_empty_comment_is_rejected()
    {
        var client = await factory.AsAsync("chi");
        var post = await TheMostCommentedPostAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/posts/{post.Id}/comments",
            new CreateCommentRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Commenting_on_an_unknown_post_is_a_404()
    {
        var client = await factory.AsAsync("chi");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/posts/{Guid.NewGuid()}/comments",
            new CreateCommentRequest("Into the void"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
