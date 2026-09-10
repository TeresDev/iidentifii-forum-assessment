using Forum.Domain.Enums;
using Forum.Infrastructure.Persistence;
using Forum.Infrastructure.Persistence.Seeding;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Forum.IntegrationTests.Persistence;

/// <summary>
/// The demo depends on the seed having particular shapes — ties in like count, enough
/// posts to page, an uneven author spread. These assert those properties rather than
/// exact totals, so the data can be adjusted without the tests becoming noise.
/// </summary>
public class SeedDataTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<ForumDbContext>(o => o.UseSqlite(_connection));
        _provider = services.BuildServiceProvider();

        await _provider.SeedForumDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private ForumDbContext Db() => _provider.GetRequiredService<ForumDbContext>();

    [Fact]
    public async Task Seeds_both_roles()
    {
        var db = Db();

        Assert.True(await db.Users.AnyAsync(u => u.Role == UserRole.Moderator));
        Assert.True(await db.Users.AnyAsync(u => u.Role == UserRole.User));
    }

    [Fact]
    public async Task No_user_has_liked_their_own_post()
    {
        var db = Db();

        var selfLikes = await db.Likes
            .Where(l => l.Post!.AuthorId == l.UserId)
            .CountAsync();

        Assert.Equal(0, selfLikes);
    }

    [Fact]
    public async Task LikeCount_matches_the_number_of_like_rows()
    {
        var db = Db();

        var mismatched = await db.Posts
            .Where(p => p.LikeCount != p.Likes.Count())
            .CountAsync();

        Assert.Equal(0, mismatched);
    }

    [Fact]
    public async Task Has_ties_in_like_count_so_unstable_paging_is_reproducible()
    {
        var db = Db();

        var tiedGroups = await db.Posts
            .GroupBy(p => p.LikeCount)
            .Where(g => g.Count() > 1)
            .CountAsync();

        Assert.True(tiedGroups >= 3, $"expected at least 3 tied like counts, found {tiedGroups}");
    }

    [Fact]
    public async Task Has_enough_posts_to_page_and_an_author_with_more_than_one_page()
    {
        var db = Db();

        Assert.True(await db.Posts.CountAsync() >= 21, "need at least 3 pages at pageSize 10");

        var busiestAuthor = await db.Posts
            .GroupBy(p => p.AuthorId)
            .Select(g => g.Count())
            .OrderByDescending(c => c)
            .FirstAsync();

        Assert.True(busiestAuthor > 10, $"need an author with >10 posts to page a filter, found {busiestAuthor}");
    }

    [Fact]
    public async Task Has_posts_with_no_comments_and_one_that_needs_comment_paging()
    {
        var db = Db();

        Assert.True(await db.Posts.CountAsync(p => !p.Comments.Any()) > 0);
        Assert.True(await db.Posts.CountAsync(p => p.Comments.Count() > 10) > 0);
    }

    [Fact]
    public async Task Flags_some_posts_as_misleading()
    {
        var db = Db();

        var flagged = await db.PostTags.CountAsync(pt => pt.TagSlug == SeedData.MisleadingTag);

        Assert.True(flagged > 0);
    }

    [Fact]
    public async Task Seeding_twice_does_not_duplicate()
    {
        var db = Db();
        var before = await db.Posts.CountAsync();

        await _provider.SeedForumDatabaseAsync();

        Assert.Equal(before, await Db().Posts.CountAsync());
    }
}
