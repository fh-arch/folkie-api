using System.Text.Json;
using Folkie.Domain.Common;

namespace Folkie.Domain.Notifications;

public class Notification : Entity
{
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Body { get; private set; }
    public string? DataJson { get; private set; }
    public bool IsRead { get; private set; }

    private Notification() { }

    public static Notification Create(Guid userId, string type, string title, string? body = null, object? data = null)
    {
        return new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            DataJson = data is null ? null : JsonSerializer.Serialize(data),
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
        Touch();
    }
}
