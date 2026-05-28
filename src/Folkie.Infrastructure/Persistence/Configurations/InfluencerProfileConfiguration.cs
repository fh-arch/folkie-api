using Folkie.Domain.Common;
using Folkie.Domain.Influencers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Folkie.Infrastructure.Persistence.Configurations;

public class InfluencerProfileConfiguration : IEntityTypeConfiguration<InfluencerProfile>
{
    public void Configure(EntityTypeBuilder<InfluencerProfile> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId).IsUnique();
        b.Property(x => x.TiktokHandle).HasMaxLength(255);
        b.HasIndex(x => x.TiktokHandle).IsUnique();
        b.Property(x => x.TiktokUserId).HasMaxLength(255);
        b.HasIndex(x => x.TiktokUserId).IsUnique();
        b.Property(x => x.City).HasMaxLength(100);
        b.Property(x => x.Country).HasMaxLength(2).HasDefaultValue("TR");
        b.Property(x => x.Bio).HasMaxLength(1000);
        b.Property(x => x.IbanName).HasMaxLength(255);
        b.Property(x => x.EngagementRate).HasPrecision(5, 2);
        b.Property(x => x.FakeFollowerScore).HasPrecision(5, 2);

        // EncryptedString → tek string kolon (value converter)
        b.Property(x => x.Iban)
            .HasColumnName("iban_encrypted")
            .HasMaxLength(1024)
            .HasConversion(
                v => v == null ? null : v.Cipher,
                v => v == null ? null : new EncryptedString(v));

        b.Property(x => x.TiktokAccessToken)
            .HasColumnName("tiktok_access_token")
            .HasMaxLength(2048)
            .HasConversion(
                v => v == null ? null : v.Cipher,
                v => v == null ? null : new EncryptedString(v));

        b.Property(x => x.TiktokRefreshToken)
            .HasColumnName("tiktok_refresh_token")
            .HasMaxLength(2048)
            .HasConversion(
                v => v == null ? null : v.Cipher,
                v => v == null ? null : new EncryptedString(v));

        b.Property(x => x.TiktokAvatarUrl).HasMaxLength(2048);

        // List<string> kolonları PostgreSQL text[]'e map'le
        b.Property(x => x.ContentLanguage).HasColumnType("text[]");
        b.Property(x => x.Categories).HasColumnType("text[]");
        b.Property(x => x.Subcategories).HasColumnType("text[]");

        // Tier domain'de hesaplanan property — DB'de saklanmaz
        b.Ignore(x => x.Tier);
    }
}
