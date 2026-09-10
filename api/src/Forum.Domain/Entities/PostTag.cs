namespace Forum.Domain.Entities;

/// <summary>
/// Carries who tagged and when. The brief cites a regulatory motivation for flagging
/// content, so the moderation action needs an audit trail rather than just a flag.
/// </summary>
public class PostTag
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }
    public required string TagSlug { get; set; }
    public Tag? Tag { get; set; }
    public Guid TaggedByUserId { get; set; }
    public User? TaggedBy { get; set; }
    public DateTime TaggedAtUtc { get; set; }
}
