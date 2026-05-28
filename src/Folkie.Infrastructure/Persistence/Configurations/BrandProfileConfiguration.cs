using Folkie.Domain.Brands;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Folkie.Infrastructure.Persistence.Configurations;

public class BrandProfileConfiguration : IEntityTypeConfiguration<BrandProfile>
{
    public void Configure(EntityTypeBuilder<BrandProfile> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId).IsUnique();
        b.Property(x => x.BrandName).IsRequired().HasMaxLength(255);
        b.Property(x => x.TaxId).HasMaxLength(50);
        b.Property(x => x.Industry).HasMaxLength(100);
        b.Property(x => x.Website).HasMaxLength(2048);
        b.Property(x => x.LogoUrl).HasMaxLength(2048);
        b.Property(x => x.ContactName).HasMaxLength(255);
        b.Property(x => x.ContactPhone).HasMaxLength(50);
        b.Property(x => x.BillingAddress).HasMaxLength(500);
    }
}
