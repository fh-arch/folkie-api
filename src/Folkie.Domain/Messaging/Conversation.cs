using Folkie.Domain.Common;

namespace Folkie.Domain.Messaging;

/// <summary>
/// İki kullanıcı (genelde marka + creator) arasında bir kampanya bağlamında konuşma.
/// </summary>
public class Conversation : Entity
{
    public Guid BrandUserId { get; private set; }
    public Guid CreatorUserId { get; private set; }
    public Guid? CampaignId { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public DateTimeOffset LastMessageAt { get; private set; }

    private Conversation() { }

    public static Conversation Create(
        Guid brandUserId,
        Guid creatorUserId,
        string subject,
        Guid? campaignId = null)
    {
        return new Conversation
        {
            BrandUserId = brandUserId,
            CreatorUserId = creatorUserId,
            CampaignId = campaignId,
            Subject = subject,
            LastMessageAt = DateTimeOffset.UtcNow,
        };
    }

    public void TouchLastMessage()
    {
        LastMessageAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public bool HasParticipant(Guid userId) =>
        BrandUserId == userId || CreatorUserId == userId;
}
