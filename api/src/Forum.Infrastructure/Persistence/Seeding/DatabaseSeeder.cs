using Forum.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Forum.Infrastructure.Persistence.Seeding;

public static class DatabaseSeeder
{
    public static async Task SeedForumDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ForumDbContext>();

        await db.Database.MigrateAsync(ct);

        if (await db.Users.AnyAsync(ct))
        {
            return;
        }

        var hasher = new PasswordHasher<User>();

        var users = BuildUsers(hasher);
        var tags = SeedData.Tags.Select(t => new Tag { Slug = t.Slug, DisplayName = t.DisplayName }).ToArray();
        var posts = BuildPosts(users);
        var comments = BuildComments(posts, users);
        var likes = BuildLikes(posts, users);
        var postTags = BuildPostTags(posts, users[0]);

        db.AddRange(users);
        db.AddRange(tags);
        db.AddRange(posts);
        db.AddRange(comments);
        db.AddRange(likes);
        db.AddRange(postTags);
        await db.SaveChangesAsync(ct);

        // The seeder is the one place LikeCount is set outside a like transaction. Doing it
        // as a single set-based pass after the rows exist keeps it consistent with them by
        // construction, rather than trusting two hand-maintained numbers to agree.
        foreach (var post in posts)
        {
            var count = likes.Count(l => l.PostId == post.Id);
            await db.Posts.Where(p => p.Id == post.Id)
                          .ExecuteUpdateAsync(s => s.SetProperty(p => p.LikeCount, count), ct);
        }
    }

    private static Guid DeterministicId(string prefix, int n) =>
        new($"{prefix}-0000-0000-0000-{n:D12}");

    private static User[] BuildUsers(IPasswordHasher<User> hasher) =>
        [.. SeedData.Users.Select((u, i) =>
        {
            var user = new User
            {
                Id = DeterministicId("11111111", i),
                Username = u.Username,
                Role = u.Role,
                PasswordHash = string.Empty,
                CreatedAtUtc = SeedData.AnchorUtc.AddDays(-60)
            };

            user.PasswordHash = hasher.HashPassword(
                user,
                u.Role == Domain.Enums.UserRole.Moderator ? SeedData.ModeratorPassword : SeedData.DefaultPassword);

            return user;
        })];

    private static Post[] BuildPosts(User[] users)
    {
        // Expand the per-author post counts into an author for each post index.
        var authorPerPost = SeedData.Users
            .SelectMany((u, i) => Enumerable.Repeat(users[i], u.PostCount))
            .ToArray();

        return [.. SeedData.PostTitles.Select((title, i) => new Post
        {
            Id = DeterministicId("22222222", i),
            AuthorId = authorPerPost[i].Id,
            Title = title,
            Body = BuildBody(title, i),
            // Oldest first, one post per day working back from the anchor.
            CreatedAtUtc = SeedData.AnchorUtc.AddDays(i - SeedData.PostTitles.Length).AddHours(i % 7)
        })];
    }

    private static string BuildBody(string title, int index) =>
        $"{title}.\n\n" +
        "We hit this while wiring up the integration last week and wanted to check how others " +
        "are handling it before we settle on an approach. Details and a minimal reproduction " +
        $"are below.\n\nReference: case #{1000 + index}.";

    private static Comment[] BuildComments(Post[] posts, User[] users)
    {
        var comments = new List<Comment>();
        var n = 0;

        for (var p = 0; p < posts.Length; p++)
        {
            for (var c = 0; c < SeedData.CommentCounts[p]; c++)
            {
                var author = users[(p + c + 1) % users.Length];
                comments.Add(new Comment
                {
                    Id = DeterministicId("33333333", n++),
                    PostId = posts[p].Id,
                    AuthorId = author.Id,
                    Body = $"Comment {c + 1}: we saw the same thing and worked around it for now.",
                    CreatedAtUtc = posts[p].CreatedAtUtc.AddHours(c + 1)
                });
            }
        }

        return [.. comments];
    }

    private static Like[] BuildLikes(Post[] posts, User[] users)
    {
        var likes = new List<Like>();

        for (var p = 0; p < posts.Length; p++)
        {
            // Authors cannot like their own post, so skip them when picking likers.
            var likers = users.Where(u => u.Id != posts[p].AuthorId)
                              .Take(SeedData.LikeCounts[p]);

            likes.AddRange(likers.Select(u => new Like
            {
                PostId = posts[p].Id,
                UserId = u.Id,
                CreatedAtUtc = posts[p].CreatedAtUtc.AddHours(2)
            }));
        }

        return [.. likes];
    }

    private static PostTag[] BuildPostTags(Post[] posts, User moderator)
    {
        var postTags = new List<PostTag>();

        // Topic tags on the first twenty posts, cycling through the non-moderation slugs.
        var topicSlugs = SeedData.Tags.Skip(1).Select(t => t.Slug).ToArray();
        for (var p = 0; p < 20; p++)
        {
            postTags.Add(new PostTag
            {
                PostId = posts[p].Id,
                TagSlug = topicSlugs[p % topicSlugs.Length],
                TaggedByUserId = moderator.Id,
                TaggedAtUtc = posts[p].CreatedAtUtc.AddHours(3)
            });
        }

        foreach (var p in SeedData.FlaggedPostIndexes)
        {
            postTags.Add(new PostTag
            {
                PostId = posts[p].Id,
                TagSlug = SeedData.MisleadingTag,
                TaggedByUserId = moderator.Id,
                TaggedAtUtc = posts[p].CreatedAtUtc.AddDays(1)
            });
        }

        return [.. postTags];
    }
}
