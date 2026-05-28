using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Creator.ConnectTiktok;

public sealed record ConnectTiktokCommand(string Code, string RedirectUri) : IRequest;

public sealed class ConnectTiktokHandler : IRequestHandler<ConnectTiktokCommand>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ITiktokApiService _tiktok;
    private readonly IIbanProtector _protector;

    public ConnectTiktokHandler(
        IFolkieDbContext db,
        ICurrentUser currentUser,
        ITiktokApiService tiktok,
        IIbanProtector protector)
    {
        _db = db;
        _currentUser = currentUser;
        _tiktok = tiktok;
        _protector = protector;
    }

    public async Task Handle(ConnectTiktokCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Influencer)
            throw new ForbiddenException("Sadece creator rolündeki kullanıcılar TikTok bağlayabilir.");

        var profile = await _db.InfluencerProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct)
            ?? throw new NotFoundException("Creator profili");

        var tokens = await _tiktok.ExchangeCodeAsync(cmd.Code, cmd.RedirectUri, ct);
        var info = await _tiktok.GetUserInfoAsync(tokens.AccessToken, ct);

        decimal engagement = info.VideoCount > 0 && info.FollowerCount > 0
            ? Math.Round((decimal)info.LikesCount / info.VideoCount / info.FollowerCount * 100, 2)
            : 0;
        engagement = Math.Min(engagement, 100);

        profile.ConnectTiktok(info.DisplayName, info.OpenId, info.FollowerCount, engagement);

        profile.StoreTiktokOAuth(
            _protector.Protect(tokens.AccessToken),
            tokens.RefreshToken != null ? _protector.Protect(tokens.RefreshToken) : null,
            tokens.ExpiresAt,
            info.AvatarUrl,
            info.LikesCount,
            info.VideoCount);

        await _db.SaveChangesAsync(ct);
    }
}
