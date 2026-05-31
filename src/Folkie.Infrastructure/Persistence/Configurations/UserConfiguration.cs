using Folkie.Domain.Common;
using Folkie.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Folkie.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.ClerkUserId).IsRequired().HasMaxLength(255);
        b.HasIndex(x => x.ClerkUserId).IsUnique();
        b.Property(x => x.Email).IsRequired().HasMaxLength(320);
        b.HasIndex(x => x.Email);
        b.Property(x => x.Role).HasConversion<int>();
        b.Property(x => x.FullName).HasMaxLength(255);
        b.Property(x => x.AvatarUrl).HasMaxLength(2048);
        b.Property(x => x.IsBlocked).HasDefaultValue(false);
        b.Property(x => x.BlockedReason).HasMaxLength(1000);
    }
}
