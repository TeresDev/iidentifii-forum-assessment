namespace Forum.Domain.Entities;

public class Tag
{
    public required string Slug { get; set; }
    public required string DisplayName { get; set; }

    public ICollection<PostTag> PostTags { get; set; } = [];
}
