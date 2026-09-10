using Forum.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forum.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username).IsRequired().HasMaxLength(32);
        builder.Property(u => u.PasswordHash).IsRequired();

        // Stored as text so the column is readable and stable if enum values are reordered.
        // This governs storage only — the JSON wire format is set separately in Program.cs.
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(16);

        builder.HasIndex(u => u.Username).IsUnique();

        builder.Ignore(u => u.IsModerator);
    }
}
