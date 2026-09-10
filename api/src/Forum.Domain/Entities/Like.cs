namespace Forum.Domain.Entities;

/// <summary>
/// Keyed on (PostId, UserId) rather than a surrogate id. The composite key is what
/// actually enforces one-like-per-user: a concurrent second insert violates it at the
/// database rather than losing a check-then-insert race in application code.
/// </summary>
public class Like
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
