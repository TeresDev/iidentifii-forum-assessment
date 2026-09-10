using System.Net;
using System.Net.Http.Json;
using Forum.Application.Contracts;

namespace Forum.IntegrationTests;

public class LikeRulesTests(ForumApiFactory factory) : IClassFixture<ForumApiFactory>
{
    /// <summary>
    /// Each test acts on a post it created itself. Reusing seeded posts made these tests
    /// order-dependent: two tests picking the same post with the same user meant whichever
    /// ran second hit a duplicate-like it did not expect. Isolation belongs in the fixture,
    /// not in carefully allocating usernames.
    /// </summary>
    private async Task<PostDetail> AFreshPostByAsync(string author)
    {
        var client = await factory.AsAsync(author);

        var response = await client.PostAsJsonAsync(
            "/api/v1/posts",
            new CreatePostRequest(
                $"Isolation post {Guid.NewGuid():N}"[..40],
                "Created by a test so it owns its own like state."));

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<PostDetail>(JsonDefaults.Options))!;
    }

    [Fact]
    public async Task Liking_requires_authentication()
    {
        var post = await AFreshPostByAsync("alice");

        var response = await factory.CreateClient().PostAsync($"/api/v1/posts/{post.Id}/likes", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_user_can_like_another_users_post()
    {
        var post = await AFreshPostByAsync("alice");
        var ben = await factory.AsAsync("ben");

        var response = await ben.PostAsync($"/api/v1/posts/{post.Id}/likes", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LikeCountResponse>(JsonDefaults.Options);
        Assert.Equal(1, body!.LikeCount);
    }

    [Fact]
    public async Task An_author_cannot_like_their_own_post()
    {
        var post = await AFreshPostByAsync("chi");
        var chi = await factory.AsAsync("chi");

        var response = await chi.PostAsync($"/api/v1/posts/{post.Id}/likes", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("\"code\":\"self-like\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Liking_the_same_post_twice_is_rejected()
    {
        var post = await AFreshPostByAsync("alice");
        var eli = await factory.AsAsync("eli");

        var first = await eli.PostAsync($"/api/v1/posts/{post.Id}/likes", null);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await eli.PostAsync($"/api/v1/posts/{post.Id}/likes", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Contains("\"code\":\"duplicate-like\"", await second.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_rejected_second_like_does_not_change_the_count()
    {
        var post = await AFreshPostByAsync("alice");
        var fay = await factory.AsAsync("fay");

        await fay.PostAsync($"/api/v1/posts/{post.Id}/likes", null);
        await fay.PostAsync($"/api/v1/posts/{post.Id}/likes", null);

        var detail = await fay.GetFromJsonAsync<PostDetail>(
            $"/api/v1/posts/{post.Id}", JsonDefaults.Options);

        Assert.Equal(1, detail!.LikeCount);
    }

    [Fact]
    public async Task Different_users_can_each_like_the_same_post()
    {
        var post = await AFreshPostByAsync("alice");
        var ben = await factory.AsAsync("ben");
        var chi = await factory.AsAsync("chi");

        await ben.PostAsync($"/api/v1/posts/{post.Id}/likes", null);
        var second = await chi.PostAsync($"/api/v1/posts/{post.Id}/likes", null);

        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var body = await second.Content.ReadFromJsonAsync<LikeCountResponse>(JsonDefaults.Options);
        Assert.Equal(2, body!.LikeCount);
    }

    [Fact]
    public async Task Unliking_removes_the_like_and_decrements_the_count()
    {
        var post = await AFreshPostByAsync("alice");
        var gus = await factory.AsAsync("gus");

        await gus.PostAsync($"/api/v1/posts/{post.Id}/likes", null);
        var response = await gus.DeleteAsync($"/api/v1/posts/{post.Id}/likes");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var detail = await gus.GetFromJsonAsync<PostDetail>(
            $"/api/v1/posts/{post.Id}", JsonDefaults.Options);

        Assert.Equal(0, detail!.LikeCount);
        Assert.False(detail.LikedByCurrentUser);
    }

    [Fact]
    public async Task Unliking_something_never_liked_is_not_an_error()
    {
        var post = await AFreshPostByAsync("alice");
        var chi = await factory.AsAsync("chi");

        // DELETE is idempotent by HTTP semantics: the caller asked for the like to be
        // absent, and afterwards it is.
        var response = await chi.DeleteAsync($"/api/v1/posts/{post.Id}/likes");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Unliking_never_drives_the_count_negative()
    {
        var post = await AFreshPostByAsync("alice");
        var chi = await factory.AsAsync("chi");

        await chi.DeleteAsync($"/api/v1/posts/{post.Id}/likes");
        await chi.DeleteAsync($"/api/v1/posts/{post.Id}/likes");

        var detail = await chi.GetFromJsonAsync<PostDetail>(
            $"/api/v1/posts/{post.Id}", JsonDefaults.Options);

        Assert.Equal(0, detail!.LikeCount);
    }

    [Fact]
    public async Task Liking_a_post_that_does_not_exist_is_a_404()
    {
        var ben = await factory.AsAsync("ben");

        var response = await ben.PostAsync($"/api/v1/posts/{Guid.NewGuid()}/likes", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_liked_post_reports_liked_by_current_user()
    {
        var post = await AFreshPostByAsync("alice");
        var dana = await factory.AsAsync("dana");

        await dana.PostAsync($"/api/v1/posts/{post.Id}/likes", null);

        var detail = await dana.GetFromJsonAsync<PostDetail>(
            $"/api/v1/posts/{post.Id}", JsonDefaults.Options);

        Assert.True(detail!.LikedByCurrentUser);
    }

    [Fact]
    public async Task A_like_by_someone_else_does_not_show_as_yours()
    {
        var post = await AFreshPostByAsync("alice");
        var ben = await factory.AsAsync("ben");
        var chi = await factory.AsAsync("chi");

        await ben.PostAsync($"/api/v1/posts/{post.Id}/likes", null);

        var asChi = await chi.GetFromJsonAsync<PostDetail>(
            $"/api/v1/posts/{post.Id}", JsonDefaults.Options);

        Assert.Equal(1, asChi!.LikeCount);
        Assert.False(asChi.LikedByCurrentUser);
    }
}
