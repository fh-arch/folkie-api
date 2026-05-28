using Folkie.Domain.Submissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Folkie.Infrastructure.Persistence.Configurations;

public class ContentSubmissionConfiguration : IEntityTypeConfiguration<ContentSubmission>
{
    public void Configure(EntityTypeBuilder<ContentSubmission> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ApplicationId);
        b.HasIndex(x => x.Status);
        b.Property(x => x.VideoUrl).HasMaxLength(2048);
        b.Property(x => x.ExternalVideoUrl).HasMaxLength(2048);
        b.Property(x => x.RevisionNote).HasMaxLength(1000);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Hashtags).HasColumnType("text[]");
    }
}
