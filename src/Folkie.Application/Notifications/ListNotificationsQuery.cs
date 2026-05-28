using Folkie.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Notifications;

public sealed record ListNotificationsQuery(bool UnreadOnly = false, int Limit = 50)
    : IRequest<NotificationsResult>;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string? Body,
    string? DataJson,
    bool IsRead,
    DateTimeOffset CreatedAt);

public sealed record NotificationsResult(
    List<NotificationDto> Items,
    int UnreadCount);

public sealed class ListNotificationsHandler
    : IRequestHandler<ListNotificationsQuery, NotificationsResult>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListNotificationsHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<NotificationsResult> Handle(
        ListNotificationsQuery q,
        CancellationToken ct)
    {
        var user = await _currentUser.GetUserAsync(ct);
        if (user is null) return new(new(), 0);

        var query = _db.Notifications.Where(n => n.UserId == user.Id);
        if (q.UnreadOnly) query = query.Where(n => !n.IsRead);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(q.Limit)
            .Select(n => new NotificationDto(
                n.Id, n.Type, n.Title, n.Body, n.DataJson, n.IsRead, n.CreatedAt))
            .ToListAsync(ct);

        var unreadCount = await _db.Notifications
            .CountAsync(n => n.UserId == user.Id && !n.IsRead, ct);

        return new(items, unreadCount);
    }
}

public sealed record MarkNotificationReadCommand(Guid Id) : IRequest<Unit>;

public sealed class MarkNotificationReadHandler
    : IRequestHandler<MarkNotificationReadCommand, Unit>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public MarkNotificationReadHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(MarkNotificationReadCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        var n = await _db.Notifications
            .FirstOrDefaultAsync(x => x.Id == cmd.Id && x.UserId == user.Id, ct);
        if (n is not null && !n.IsRead)
        {
            n.MarkAsRead();
            await _db.SaveChangesAsync(ct);
        }
        return Unit.Value;
    }
}

public sealed record MarkAllReadCommand() : IRequest<Unit>;

public sealed class MarkAllReadHandler : IRequestHandler<MarkAllReadCommand, Unit>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public MarkAllReadHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(MarkAllReadCommand _, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        var unread = await _db.Notifications
            .Where(n => n.UserId == user.Id && !n.IsRead)
            .ToListAsync(ct);
        foreach (var n in unread) n.MarkAsRead();
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
