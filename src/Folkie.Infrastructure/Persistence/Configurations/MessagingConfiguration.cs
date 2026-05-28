using Folkie.Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Folkie.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.BrandUserId, x.CreatorUserId, x.CampaignId });
        b.HasIndex(x => x.LastMessageAt);
        b.Property(x => x.Subject).IsRequired().HasMaxLength(255);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ConversationId, x.CreatedAt });
        b.HasIndex(x => new { x.ConversationId, x.IsRead });
        b.Property(x => x.Body).IsRequired().HasMaxLength(5000);
    }
}
