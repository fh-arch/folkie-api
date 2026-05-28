using Folkie.Domain.Common;
using Folkie.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Folkie.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.CampaignId);
        b.HasIndex(x => x.InfluencerProfileId);
        b.HasIndex(x => x.Status);
        b.Property(x => x.Amount).HasPrecision(10, 2);
        b.Property(x => x.PaymentType).HasConversion<int>();
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.IbanName).IsRequired().HasMaxLength(255);
        b.Property(x => x.AdminNote).HasMaxLength(1000);
        b.Property(x => x.TransferReference).HasMaxLength(100);

        b.Property(x => x.Iban)
            .HasColumnName("iban_encrypted")
            .IsRequired()
            .HasMaxLength(1024)
            .HasConversion(
                v => v.Cipher,
                v => new EncryptedString(v));
    }
}

public class BrandPaymentConfiguration : IEntityTypeConfiguration<BrandPayment>
{
    public void Configure(EntityTypeBuilder<BrandPayment> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.CampaignId);
        b.HasIndex(x => x.BrandProfileId);
        b.HasIndex(x => x.Status);
        b.Property(x => x.Amount).HasPrecision(10, 2);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.PaymentMethod).HasMaxLength(50).HasDefaultValue("bank_transfer");
        b.Property(x => x.TransferReference).HasMaxLength(100);
        b.Property(x => x.ReceiptUrl).HasMaxLength(2048);
        b.Property(x => x.AdminNote).HasMaxLength(1000);
    }
}
