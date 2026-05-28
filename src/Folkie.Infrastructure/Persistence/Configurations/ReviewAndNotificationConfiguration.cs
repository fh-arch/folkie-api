using Folkie.Domain.Notifications;
using Folkie.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Folkie.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.CampaignId, x.ReviewerId }).IsUnique();
        b.Property(x => x.ReviewerRole).HasConversion<int>();
        b.Property(x => x.Comment).HasMaxLength(2000);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UserId, x.IsRead });
        b.Property(x => x.Type).IsRequired().HasMaxLength(100);
        b.Property(x => x.Title).IsRequired().HasMaxLength(255);
        b.Property(x => x.Body).HasMaxLength(2000);
        b.Property(x => x.DataJson).HasColumnType("jsonb");
    }
}
