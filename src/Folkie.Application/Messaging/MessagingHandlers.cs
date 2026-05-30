using FluentValidation;
using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Messaging;

/* ─── List conversations ───────────────────────────────────── */

public sealed record ListConversationsQuery() : IRequest<List<ConversationDto>>;

public sealed record ConversationDto(
    Guid Id,
    Guid OtherUserId,
    string OtherUserName,
    string? OtherAvatarUrl,
    Guid? CampaignId,
    string? CampaignTitle,
    string Subject,
    string? LastMessageBody,
    DateTimeOffset LastMessageAt,
    int UnreadCount);

public sealed class ListConversationsHandler
    : IRequestHandler<ListConversationsQuery, List<ConversationDto>>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListConversationsHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<ConversationDto>> Handle(
        ListConversationsQuery _,
        CancellationToken ct)
    {
        var user = await _currentUser.GetUserAsync(ct);
        if (user is null) return new();

        var rows = await _db.Conversations
            .Where(c => c.BrandUserId == user.Id || c.CreatorUserId == user.Id)
            .OrderByDescending(c => c.LastMessageAt)
            .Select(c => new
            {
                c.Id,
                OtherId = c.BrandUserId == user.Id ? c.CreatorUserId : c.BrandUserId,
                c.CampaignId,
                c.Subject,
                c.LastMessageAt,
                LastMessageBody = _db.Messages
                    .Where(m => m.ConversationId == c.Id)
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.Body)
                    .FirstOrDefault(),
                UnreadCount = _db.Messages
                    .Count(m => m.ConversationId == c.Id
                        && m.SenderUserId != user.Id
                        && !m.IsRead),
            })
            .ToListAsync(ct);

        // Resolve other user info + campaign title
        var otherIds = rows.Select(r => r.OtherId).Distinct().ToArray();
        var campaignIds = rows.Where(r => r.CampaignId.HasValue)
            .Select(r => r.CampaignId!.Value).Distinct().ToArray();

        var otherUsers = await _db.Users
            .Where(u => otherIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.Email, u.AvatarUrl })
            .ToDictionaryAsync(u => u.Id, ct);

        var campaigns = await _db.Campaigns
            .Where(c => campaignIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Title, ct);

        return rows.Select(r =>
        {
            var other = otherUsers.GetValueOrDefault(r.OtherId);
            return new ConversationDto(
                r.Id,
                r.OtherId,
                other?.FullName ?? other?.Email ?? "Bilinmiyor",
                other?.AvatarUrl,
                r.CampaignId,
                r.CampaignId.HasValue ? campaigns.GetValueOrDefault(r.CampaignId.Value) : null,
                r.Subject,
                r.LastMessageBody,
                r.LastMessageAt,
                r.UnreadCount);
        }).ToList();
    }
}

/* ─── List messages in a conversation ──────────────────────── */

public sealed record ListMessagesQuery(Guid ConversationId) : IRequest<List<MessageDto>>;

public sealed record MessageDto(
    Guid Id,
    Guid SenderUserId,
    string Body,
    bool IsRead,
    bool IsMine,
    DateTimeOffset CreatedAt);

public sealed class ListMessagesHandler : IRequestHandler<ListMessagesQuery, List<MessageDto>>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListMessagesHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<MessageDto>> Handle(ListMessagesQuery q, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);

        var conv = await _db.Conversations
            .FirstOrDefaultAsync(c => c.Id == q.ConversationId, ct)
            ?? throw new NotFoundException("Konuşma");
        if (!conv.HasParticipant(user.Id))
            throw new ForbiddenException();

        var messages = await _db.Messages
            .Where(m => m.ConversationId == q.ConversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        // Mark incoming as read
        var unread = messages.Where(m => m.SenderUserId != user.Id && !m.IsRead);
        foreach (var m in unread) m.MarkAsRead();
        if (unread.Any()) await _db.SaveChangesAsync(ct);

        return messages.Select(m => new MessageDto(
            m.Id, m.SenderUserId, m.Body, m.IsRead,
            m.SenderUserId == user.Id, m.CreatedAt)).ToList();
    }
}

/* ─── Send message ─────────────────────────────────────────── */

public sealed record SendMessageCommand(Guid ConversationId, string Body) : IRequest<Guid>;

public sealed class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(5000);
    }
}

public sealed class SendMessageHandler : IRequestHandler<SendMessageCommand, Guid>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;

    public SendMessageHandler(
        IFolkieDbContext db,
        ICurrentUser currentUser,
        INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<Guid> Handle(SendMessageCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        var conv = await _db.Conversations
            .FirstOrDefaultAsync(c => c.Id == cmd.ConversationId, ct)
            ?? throw new NotFoundException("Konuşma");
        if (!conv.HasParticipant(user.Id))
            throw new ForbiddenException();

        var msg = Message.Create(conv.Id, user.Id, cmd.Body);
        conv.TouchLastMessage();
        _db.Messages.Add(msg);
        await _db.SaveChangesAsync(ct);

        // Notify the other party
        var otherUserId = conv.BrandUserId == user.Id ? conv.CreatorUserId : conv.BrandUserId;
        await _notifications.NotifyAsync(
            otherUserId,
            "message.received",
            "Yeni mesaj",
            cmd.Body.Length > 100 ? cmd.Body[..100] + "..." : cmd.Body,
            new { conversationId = conv.Id, messageId = msg.Id },
            ct);

        return msg.Id;
    }
}

/* ─── Start conversation ───────────────────────────────────── */

public sealed record StartConversationCommand(
    Guid OtherUserId,
    Guid? CampaignId,
    string? Subject,
    string? FirstMessage) : IRequest<Guid>;

public sealed class StartConversationHandler : IRequestHandler<StartConversationCommand, Guid>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public StartConversationHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(StartConversationCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        var other = await _db.Users.FirstOrDefaultAsync(u => u.Id == cmd.OtherUserId, ct)
            ?? throw new NotFoundException("Diğer kullanıcı");

        // Determine roles
        var (brandUserId, creatorUserId) =
            user.Role == Domain.Common.UserRole.Brand
                ? (user.Id, other.Id)
                : (other.Id, user.Id);

        var existing = await _db.Conversations.FirstOrDefaultAsync(c =>
            c.BrandUserId == brandUserId
            && c.CreatorUserId == creatorUserId
            && c.CampaignId == cmd.CampaignId, ct);

        if (existing is not null) return existing.Id;

        var conv = Conversation.Create(brandUserId, creatorUserId, cmd.Subject ?? "Direct Message", cmd.CampaignId);
        _db.Conversations.Add(conv);
        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(cmd.FirstMessage))
        {
            var msg = Message.Create(conv.Id, user.Id, cmd.FirstMessage);
            conv.TouchLastMessage();
            _db.Messages.Add(msg);
            await _db.SaveChangesAsync(ct);
        }

        return conv.Id;
    }
}
