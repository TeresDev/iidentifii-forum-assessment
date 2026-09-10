using Forum.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forum.Infrastructure.Persistence.Configurations;

public class PostTagConfiguration : IEntityTypeConfiguration<PostTag>
{
    public void Configure(EntityTypeBuilder<PostTag> builder)
    {
        builder.HasKey(pt => new { pt.PostId, pt.TagSlug });

        builder.Property(pt => pt.TagSlug).HasMaxLength(64);

        builder.HasOne(pt => pt.Post)
               .WithMany(p => p.PostTags)
               .HasForeignKey(pt => pt.PostId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pt => pt.Tag)
               .WithMany(t => t.PostTags)
               .HasForeignKey(pt => pt.TagSlug)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pt => pt.TaggedBy)
               .WithMany()
               .HasForeignKey(pt => pt.TaggedByUserId)
               .OnDelete(DeleteBehavior.Restrict);

        // Drives the ?tag= filter.
        builder.HasIndex(pt => pt.TagSlug);
    }
}
