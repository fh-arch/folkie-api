using Folkie.Domain.Brands;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Folkie.Infrastructure.Persistence.Configurations;

public class BrandFavoriteConfiguration : IEntityTypeConfiguration<BrandFavorite>
{
    public void Configure(EntityTypeBuilder<BrandFavorite> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.BrandProfileId, x.InfluencerProfileId }).IsUnique();
        b.Property(x => x.Note).HasMaxLength(500);
    }
}
