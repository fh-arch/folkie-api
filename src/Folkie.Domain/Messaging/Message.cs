using Folkie.Domain.Common;

namespace Folkie.Domain.Messaging;

public class Message : Entity
{
    public Guid ConversationId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    private Message() { }

    public static Message Create(Guid conversationId, Guid senderUserId, string body)
    {
        return new Message
        {
            ConversationId = conversationId,
            SenderUserId = senderUserId,
            Body = body,
        };
    }

    public void MarkAsRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = DateTimeOffset.UtcNow;
        Touch();
    }
}
