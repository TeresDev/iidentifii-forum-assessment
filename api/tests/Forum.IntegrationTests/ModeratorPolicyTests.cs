using System.Net;
using System.Net.Http.Json;
using Forum.Application.Contracts;
using Forum.Infrastructure.Persistence.Seeding;

namespace Forum.IntegrationTests;

public class ModeratorPolicyTests(ForumApiFactory factory) : IClassFixture<ForumApiFactory>
{
    private async Task<PostDetail> AFreshPostAsync()
    {
        var client = await factory.AsAsync("alice");

        var response = await client.PostAsJsonAsync(
            "/api/v1/posts",
            new CreatePostRequest(
                $"Taggable post {Guid.NewGuid():N}"[..40],
                "Created by a test so it owns its own tag state."));

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<PostDetail>(JsonDefaults.Options))!;
    }

    // The control that matters. Hiding the tag button in the browser is presentation;
    // this asserts the server refuses a regular user calling the endpoint directly,
    // which is exactly what a third-party consumer with a valid token would be doing.
    [Fact]
    public async Task A_regular_user_cannot_tag_a_post()
    {
        var post = await AFreshPostAsync();
        var regularUser = await factory.AsAsync("ben");

        var response = await regularUser.PostAsJsonAsync(
            $"/api/v1/posts/{post.Id}/tags",
            new AddTagRequest(SeedData.MisleadingTag));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_regular_user_cannot_remove_a_tag()
    {
        var post = await AFreshPostAsync();
        var moderator = await factory.AsAsync("mod.jordan");
        await moderator.PostAsJsonAsync(
            $"/api/v1/posts/{post.Id}/tags", new AddTagRequest(SeedData.MisleadingTag));

        var regularUser = await factory.AsAsync("chi");
        var response = await regularUser.DeleteAsync(
            $"/api/v1/posts/{post.Id}/tags/{SeedData.MisleadingTag}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Tagging_anonymously_is_unauthorised_not_forbidden()
    {
        var post = await AFreshPostAsync();

        var response = await factory.CreateClient().PostAsJsonAsync(
            $"/api/v1/posts/{post.Id}/tags",
            new AddTagRequest(SeedData.MisleadingTag));

        // 401 means "we do not know who you are", 403 means "we do, and you may not".
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_moderator_can_tag_a_post_as_misleading()
    {
        var post = await AFreshPostAsync();
        var moderator = await factory.AsAsync("mod.jordan");

        var response = await moderator.PostAsJsonAsync(
            $"/api/v1/posts/{post.Id}/tags",
            new AddTagRequest(SeedData.MisleadingTag));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var detail = await moderator.GetFromJsonAsync<PostDetail>(
            $"/api/v1/posts/{post.Id}", JsonDefaults.Options);

        Assert.Contains(detail!.Tags, t => t.Slug == SeedData.MisleadingTag);
    }

    [Fact]
    public async Task Applying_the_same_tag_twice_is_rejected()
    {
        var post = await AFreshPostAsync();
        var moderator = await factory.AsAsync("mod.jordan");

        await moderator.PostAsJsonAsync(
            $"/api/v1/posts/{post.Id}/tags", new AddTagRequest(SeedData.MisleadingTag));

        var second = await moderator.PostAsJsonAsync(
            $"/api/v1/posts/{post.Id}/tags", new AddTagRequest(SeedData.MisleadingTag));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Contains("\"code\":\"already-tagged\"", await second.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_moderator_can_remove_a_tag()
    {
        var post = await AFreshPostAsync();
        var moderator = await factory.AsAsync("mod.jordan");

        await moderator.PostAsJsonAsync(
            $"/api/v1/posts/{post.Id}/tags", new AddTagRequest(SeedData.MisleadingTag));

        var response = await moderator.DeleteAsync(
            $"/api/v1/posts/{post.Id}/tags/{SeedData.MisleadingTag}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var detail = await moderator.GetFromJsonAsync<PostDetail>(
            $"/api/v1/posts/{post.Id}", JsonDefaults.Options);

        Assert.DoesNotContain(detail!.Tags, t => t.Slug == SeedData.MisleadingTag);
    }

    [Fact]
    public async Task Tagging_an_unknown_post_is_a_404()
    {
        var moderator = await factory.AsAsync("mod.jordan");

        var response = await moderator.PostAsJsonAsync(
            $"/api/v1/posts/{Guid.NewGuid()}/tags",
            new AddTagRequest(SeedData.MisleadingTag));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Applying_a_tag_outside_the_vocabulary_is_a_404()
    {
        var post = await AFreshPostAsync();
        var moderator = await factory.AsAsync("mod.jordan");

        // The vocabulary is closed and seeded, so an arbitrary slug is not something a
        // moderator may invent.
        var response = await moderator.PostAsJsonAsync(
            $"/api/v1/posts/{post.Id}/tags",
            new AddTagRequest("not-a-real-tag"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_tagged_post_is_reachable_through_the_tag_filter()
    {
        var post = await AFreshPostAsync();
        var moderator = await factory.AsAsync("mod.jordan");

        await moderator.PostAsJsonAsync(
            $"/api/v1/posts/{post.Id}/tags", new AddTagRequest(SeedData.MisleadingTag));

        var filtered = await factory.CreateClient().GetFromJsonAsync<PagedResult<PostSummary>>(
            $"/api/v1/posts?pageSize=50&tag={SeedData.MisleadingTag}", JsonDefaults.Options);

        Assert.Contains(filtered!.Items, p => p.Id == post.Id);
    }
}
