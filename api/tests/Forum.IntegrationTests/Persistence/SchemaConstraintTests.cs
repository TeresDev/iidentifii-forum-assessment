using Forum.Domain.Entities;
using Forum.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Forum.IntegrationTests.Persistence;

/// <summary>
/// Runs against real SQLite rather than the in-memory provider, because the in-memory
/// provider does not enforce key constraints — the very thing under test here.
/// </summary>
public class SchemaConstraintTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private ForumDbContext _db = null!;

    public async Task InitializeAsync()
    {
        // A held-open :memory: connection is what keeps the schema alive for the test's
        // lifetime; closing it drops the database.
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        _db = new ForumDbContext(
            new DbContextOptionsBuilder<ForumDbContext>().UseSqlite(_connection).Options);

        await _db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task<(User author, User liker, Post post)> SeedPostAsync()
    {
        var author = new User { Id = Guid.NewGuid(), Username = "author", PasswordHash = "x" };
        var liker = new User { Id = Guid.NewGuid(), Username = "liker", PasswordHash = "x" };
        var post = new Post { Id = Guid.NewGuid(), AuthorId = author.Id, Title = "t", Body = "b" };

        _db.AddRange(author, liker, post);
        await _db.SaveChangesAsync();

        return (author, liker, post);
    }

    [Fact]
    public async Task A_second_like_by_the_same_user_violates_the_composite_key()
    {
        var (_, liker, post) = await SeedPostAsync();

        _db.Add(new Like { PostId = post.Id, UserId = liker.Id });
        await _db.SaveChangesAsync();

        // A different DbContext, because the first one already tracks the existing row.
        // This models two independent requests racing, which is the case the key defends.
        await using var second = new ForumDbContext(
            new DbContextOptionsBuilder<ForumDbContext>().UseSqlite(_connection).Options);

        second.Add(new Like { PostId = post.Id, UserId = liker.Id });

        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Two_different_users_may_like_the_same_post()
    {
        var (author, liker, post) = await SeedPostAsync();
        var other = new User { Id = Guid.NewGuid(), Username = "other", PasswordHash = "x" };
        _db.Add(other);
        await _db.SaveChangesAsync();

        _db.Add(new Like { PostId = post.Id, UserId = liker.Id });
        _db.Add(new Like { PostId = post.Id, UserId = other.Id });
        await _db.SaveChangesAsync();

        Assert.Equal(2, await _db.Likes.CountAsync(l => l.PostId == post.Id));
        Assert.NotEqual(author.Id, liker.Id);
    }

    [Fact]
    public async Task Usernames_are_unique()
    {
        _db.Add(new User { Id = Guid.NewGuid(), Username = "taken", PasswordHash = "x" });
        await _db.SaveChangesAsync();

        await using var second = new ForumDbContext(
            new DbContextOptionsBuilder<ForumDbContext>().UseSqlite(_connection).Options);

        second.Add(new User { Id = Guid.NewGuid(), Username = "taken", PasswordHash = "x" });

        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task A_post_cannot_carry_the_same_tag_twice()
    {
        var (author, _, post) = await SeedPostAsync();
        _db.Add(new Tag { Slug = "misleading-information", DisplayName = "Misleading" });
        await _db.SaveChangesAsync();

        _db.Add(new PostTag
        {
            PostId = post.Id,
            TagSlug = "misleading-information",
            TaggedByUserId = author.Id
        });
        await _db.SaveChangesAsync();

        await using var second = new ForumDbContext(
            new DbContextOptionsBuilder<ForumDbContext>().UseSqlite(_connection).Options);

        second.Add(new PostTag
        {
            PostId = post.Id,
            TagSlug = "misleading-information",
            TaggedByUserId = author.Id
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }
}
