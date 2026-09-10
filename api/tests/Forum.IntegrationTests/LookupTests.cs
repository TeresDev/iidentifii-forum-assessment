using System.Net;
using System.Net.Http.Json;
using Forum.Application.Contracts;
using Forum.Infrastructure.Persistence.Seeding;

namespace Forum.IntegrationTests;

/// <summary>
/// These two endpoints exist so the front-end's author and tag filters have something to
/// populate from. Without them the brief's "filters (date range, author, tags)" has no
/// usable UI — you would be asking a person to type a GUID.
/// </summary>
public class LookupTests(ForumApiFactory factory) : IClassFixture<ForumApiFactory>
{
    [Fact]
    public async Task Tags_are_readable_anonymously()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/tags");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tags = await response.Content.ReadFromJsonAsync<List<TagDto>>(JsonDefaults.Options);
        Assert.Contains(tags!, t => t.Slug == SeedData.MisleadingTag);
        Assert.All(tags!, t => Assert.False(string.IsNullOrWhiteSpace(t.DisplayName)));
    }

    [Fact]
    public async Task Users_are_readable_anonymously()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var users = await response.Content.ReadFromJsonAsync<List<UserSummary>>(JsonDefaults.Options);
        Assert.Contains(users!, u => u.Username == "alice");
        Assert.Contains(users!, u => u.Role == Domain.Enums.UserRole.Moderator);
    }

    [Fact]
    public async Task The_user_list_never_exposes_password_hashes()
    {
        var raw = await factory.CreateClient().GetStringAsync("/api/v1/users");

        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AQAAAA", raw);
    }
}
