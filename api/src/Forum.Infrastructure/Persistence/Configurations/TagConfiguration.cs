using Forum.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forum.Infrastructure.Persistence.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        // The slug is the natural key. It is what the API filters on and what appears
        // in URLs, so a surrogate id would add a join without adding anything.
        builder.HasKey(t => t.Slug);

        builder.Property(t => t.Slug).HasMaxLength(64);
        builder.Property(t => t.DisplayName).IsRequired().HasMaxLength(128);
    }
}
