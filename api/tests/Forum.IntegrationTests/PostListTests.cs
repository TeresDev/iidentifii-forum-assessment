using System.Net;
using System.Net.Http.Json;
using Forum.Application.Contracts;

namespace Forum.IntegrationTests;

public class PostListTests(ForumApiFactory factory) : IClassFixture<ForumApiFactory>
{
    private async Task<PagedResult<PostSummary>> ListAsync(string query = "")
    {
        var response = await factory.CreateClient().GetAsync($"/api/v1/posts{query}");
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<PagedResult<PostSummary>>(JsonDefaults.Options))!;
    }

    [Fact]
    public async Task Browsing_posts_does_not_require_authentication()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/posts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Each of these asserts a filter returns STRICTLY FEWER rows than no filter. A
    // misspelled or unbound parameter is ignored by model binding and still returns 200
    // with the full unfiltered list, which looks like success. This is the assertion that
    // turns that silent failure into a red test.

    [Fact]
    public async Task Filtering_by_author_returns_fewer_posts()
    {
        var all = await ListAsync("?pageSize=50");
        var author = all.Items.First().AuthorId;

        var filtered = await ListAsync($"?pageSize=50&author={author}");

        Assert.True(filtered.TotalCount > 0, "expected the author to have posts");
        Assert.True(filtered.TotalCount < all.TotalCount,
            $"author filter returned {filtered.TotalCount} of {all.TotalCount} — the parameter is not bound");
        Assert.All(filtered.Items, p => Assert.Equal(author, p.AuthorId));
    }

    [Fact]
    public async Task Filtering_by_tag_returns_fewer_posts()
    {
        var all = await ListAsync("?pageSize=50");

        var filtered = await ListAsync("?pageSize=50&tag=misleading-information");

        Assert.True(filtered.TotalCount > 0, "expected some posts to carry the moderation tag");
        Assert.True(filtered.TotalCount < all.TotalCount,
            $"tag filter returned {filtered.TotalCount} of {all.TotalCount} — the parameter is not bound");
        Assert.All(filtered.Items, p => Assert.Contains(p.Tags, t => t.Slug == "misleading-information"));
    }

    [Fact]
    public async Task Filtering_by_date_range_returns_fewer_posts()
    {
        var all = await ListAsync("?pageSize=50&sort=date&dir=asc");
        var oldest = all.Items.First().CreatedAtUtc;
        var newest = all.Items.Last().CreatedAtUtc;
        var midpoint = oldest.AddTicks((newest - oldest).Ticks / 2);

        var filtered = await ListAsync($"?pageSize=50&from={midpoint:o}");

        Assert.True(filtered.TotalCount > 0);
        Assert.True(filtered.TotalCount < all.TotalCount,
            $"date filter returned {filtered.TotalCount} of {all.TotalCount} — the parameter is not bound");
        Assert.All(filtered.Items, p => Assert.True(p.CreatedAtUtc >= midpoint));
    }

    [Fact]
    public async Task The_to_bound_is_exclusive_so_adjacent_ranges_do_not_overlap()
    {
        var all = await ListAsync("?pageSize=50&sort=date&dir=asc");
        var boundary = all.Items[5].CreatedAtUtc;

        var before = await ListAsync($"?pageSize=50&to={boundary:o}");
        var fromBoundary = await ListAsync($"?pageSize=50&from={boundary:o}");

        Assert.DoesNotContain(before.Items, p => p.CreatedAtUtc >= boundary);
        Assert.Contains(fromBoundary.Items, p => p.CreatedAtUtc == boundary);
        Assert.Equal(all.TotalCount, before.TotalCount + fromBoundary.TotalCount);
    }

    [Fact]
    public async Task Paging_by_likes_is_stable_across_pages_when_counts_tie()
    {
        // The seed deliberately contains ties in LikeCount. Without the trailing Id in
        // the ORDER BY, tied rows come back in whatever order the engine chooses, so the
        // same post can appear on two pages while another appears on none.
        var everything = await ListAsync("?pageSize=50&sort=likes&dir=desc");

        var seen = new List<Guid>();
        for (var page = 1; page <= everything.TotalCount; page++)
        {
            var single = await ListAsync($"?page={page}&pageSize=1&sort=likes&dir=desc");
            seen.AddRange(single.Items.Select(p => p.Id));
        }

        Assert.Equal(everything.TotalCount, seen.Count);
        Assert.Equal(seen.Count, seen.Distinct().Count());
        Assert.Equal(everything.Items.Select(p => p.Id), seen);
    }

    [Fact]
    public async Task Paging_by_date_is_stable_across_pages()
    {
        var everything = await ListAsync("?pageSize=50&sort=date&dir=desc");

        var pageOne = await ListAsync("?page=1&pageSize=10&sort=date&dir=desc");
        var pageTwo = await ListAsync("?page=2&pageSize=10&sort=date&dir=desc");
        var pageThree = await ListAsync("?page=3&pageSize=10&sort=date&dir=desc");

        var paged = pageOne.Items.Concat(pageTwo.Items).Concat(pageThree.Items).Select(p => p.Id).ToList();

        Assert.Equal(paged.Count, paged.Distinct().Count());
        Assert.Equal(everything.Items.Select(p => p.Id), paged);
    }

    [Fact]
    public async Task Sorting_by_likes_descending_orders_by_like_count()
    {
        var result = await ListAsync("?pageSize=50&sort=likes&dir=desc");

        var counts = result.Items.Select(p => p.LikeCount).ToList();
        Assert.Equal(counts.OrderByDescending(c => c), counts);
    }

    [Fact]
    public async Task Sorting_direction_is_honoured()
    {
        var ascending = await ListAsync("?pageSize=50&sort=date&dir=asc");
        var descending = await ListAsync("?pageSize=50&sort=date&dir=desc");

        Assert.Equal(ascending.Items.Select(p => p.Id).Reverse(), descending.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task Page_size_above_the_limit_is_rejected()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/posts?pageSize=5000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Page_below_one_is_rejected()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/posts?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_callers_never_see_a_post_as_liked()
    {
        var result = await ListAsync("?pageSize=50");

        Assert.All(result.Items, p => Assert.False(p.LikedByCurrentUser));
    }

    [Fact]
    public async Task Summary_carries_a_comment_count_and_an_excerpt_not_the_full_body()
    {
        var result = await ListAsync("?pageSize=50");

        Assert.Contains(result.Items, p => p.CommentCount > 0);
        Assert.All(result.Items, p => Assert.True(p.Excerpt.Length <= 200));
    }

    [Fact]
    public async Task Total_pages_reflects_the_page_size()
    {
        var result = await ListAsync("?page=1&pageSize=10");

        Assert.Equal((int)Math.Ceiling(result.TotalCount / 10.0), result.TotalPages);
        Assert.True(result.TotalPages >= 3, "seed should provide at least three pages at pageSize 10");
    }
}
