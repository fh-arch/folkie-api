using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Folkie.Infrastructure.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly IFolkieDbContext _db;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IFolkieDbContext db,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task NotifyAsync(
        Guid userId,
        string type,
        string title,
        string? body = null,
        object? data = null,
        CancellationToken ct = default)
    {
        var notification = Notification.Create(userId, type, title, body, data);
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        // Fire-and-forget email in a new scope (no Hangfire required)
        var notificationId = notification.Id;
        _ = Task.Run(async () =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<EmailDispatcher>();
            await dispatcher.SendNotificationEmailAsync(notificationId);
        });

        _logger.LogDebug("Notification {Id} created for user {UserId}", notification.Id, userId);
    }
}

/// <summary>Hangfire ile çağrılan e-posta gönderici.</summary>
public sealed class EmailDispatcher
{
    private readonly IFolkieDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailDispatcher> _logger;

    public EmailDispatcher(
        IFolkieDbContext db,
        IEmailSender emailSender,
        ILogger<EmailDispatcher> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendNotificationEmailAsync(Guid notificationId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId);
        if (notification is null) return;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == notification.UserId);
        if (user is null || string.IsNullOrEmpty(user.Email)) return;

        var subject = $"Folkie · {notification.Title}";
        var html = $$"""
            <!DOCTYPE html>
            <html>
            <body style="font-family: -apple-system, system-ui, sans-serif; background:#F7F7FB; padding:24px;">
              <div style="max-width:560px; margin:0 auto; background:#fff; border-radius:16px; padding:32px; border:1px solid #e8e8ec;">
                <div style="font-size:24px; font-weight:700; color:#6B3DFF; margin-bottom:24px;">folkie<span style="color:#C6FF00;">.</span></div>
                <h1 style="font-size:20px; margin:0 0 12px;">{{notification.Title}}</h1>
                {{(notification.Body is null ? "" : $"<p style=\"color:#555; line-height:1.5; margin:0 0 24px;\">{notification.Body}</p>")}}
                <a href="https://folkie.com/dashboard" style="display:inline-block; background:#6B3DFF; color:#fff; padding:12px 24px; border-radius:999px; text-decoration:none; font-weight:600;">Folkie'de aç</a>
                <hr style="border:none; border-top:1px solid #eee; margin:32px 0 16px;">
                <p style="color:#999; font-size:12px;">Bu e-postayı almak istemiyorsan ayarlardan bildirim tercihlerini değiştirebilirsin.</p>
              </div>
            </body>
            </html>
            """;

        var ok = await _emailSender.SendAsync(user.Email, subject, html);
        if (!ok) _logger.LogWarning("Email gönderilemedi: notification={Id} user={UserId}", notification.Id, user.Id);
    }
}
