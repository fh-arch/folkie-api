using Folkie.Domain.Common;
using Folkie.Domain.Campaigns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Folkie.Infrastructure.Persistence.Configurations;

public class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.BrandProfileId);
        b.HasIndex(x => x.Status);
        b.Property(x => x.Title).IsRequired().HasMaxLength(255);
        b.Property(x => x.ProductName).IsRequired().HasMaxLength(255);
        b.Property(x => x.ProductCategory).IsRequired().HasMaxLength(100);
        b.Property(x => x.Brief).IsRequired();
        b.Property(x => x.Tone).HasMaxLength(50);
        b.Property(x => x.BudgetPerInfluencer).HasPrecision(10, 2);
        b.Property(x => x.PlatformFeeRate).HasPrecision(5, 2).HasDefaultValue(15.0m);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.ProductDelivery).HasConversion<int>();
        b.Property(x => x.ApprovalMode).HasConversion<int>();

        // List<ContentType> → int[] kolon
        b.Property(x => x.ContentTypes)
            .HasColumnType("integer[]")
            .HasConversion(
                v => v.Select(e => (int)e).ToArray(),
                v => v.Select(i => (ContentType)i).ToList());

        b.Property(x => x.RequiredHashtags).HasColumnType("text[]");
        b.Property(x => x.TargetCategories).HasColumnType("text[]");
        b.Property(x => x.TargetCities).HasColumnType("text[]");
        b.Property(x => x.ContentLanguage).HasColumnType("text[]");

        // TotalBudget hesaplanan property — saklamıyoruz
        b.Ignore(x => x.TotalBudget);
    }
}
