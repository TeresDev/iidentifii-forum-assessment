namespace Forum.Domain.Entities;

public class Comment
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public Post? Post { get; set; }
    public Guid AuthorId { get; set; }
    public User? Author { get; set; }
    public required string Body { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
