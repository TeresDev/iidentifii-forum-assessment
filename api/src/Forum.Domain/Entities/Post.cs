using Forum.Domain.Errors;

namespace Forum.Domain.Entities;

public class Post
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public User? Author { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    // Denormalised so sorting by popularity is an indexed column read rather than a
    // join and aggregate on every page. Maintained by ExecuteUpdateAsync in the same
    // transaction as the Like row, never through the change tracker.
    public int LikeCount { get; set; }

    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Like> Likes { get; set; } = [];
    public ICollection<PostTag> PostTags { get; set; } = [];

    public void EnsureCanBeLikedBy(Guid userId)
    {
        if (userId == AuthorId)
        {
            throw new SelfLikeException();
        }
    }
}
