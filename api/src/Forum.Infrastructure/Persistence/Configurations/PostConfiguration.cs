using Forum.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forum.Infrastructure.Persistence.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Body).IsRequired().HasMaxLength(10_000);

        builder.HasOne(p => p.Author)
               .WithMany(u => u.Posts)
               .HasForeignKey(p => p.AuthorId)
               .OnDelete(DeleteBehavior.Restrict);

        // Both sort keys carry Id as a trailing column. Sorting on a non-unique column
        // alone leaves ties in undefined order, so rows repeat on one page and vanish
        // from another as you page through. The trailing Id makes the order total.
        builder.HasIndex(p => new { p.CreatedAtUtc, p.Id }).IsDescending(true, true);
        builder.HasIndex(p => new { p.LikeCount, p.Id }).IsDescending(true, true);

        builder.HasIndex(p => p.AuthorId);
    }
}
