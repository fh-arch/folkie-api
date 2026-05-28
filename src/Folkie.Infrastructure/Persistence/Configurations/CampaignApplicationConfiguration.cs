using Folkie.Domain.Applications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Folkie.Infrastructure.Persistence.Configurations;

public class CampaignApplicationConfiguration : IEntityTypeConfiguration<CampaignApplication>
{
    public void Configure(EntityTypeBuilder<CampaignApplication> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.CampaignId, x.InfluencerProfileId }).IsUnique();
        b.HasIndex(x => x.Status);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.RejectionReason).HasMaxLength(1000);
    }
}
