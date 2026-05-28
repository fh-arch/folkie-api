using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Creator.ConnectTiktok;

/// <summary>
/// Sandbox / demo mode: connects a mock TikTok account without calling the real TikTok API.
/// The endpoint layer is responsible for checking sandbox mode is enabled.
/// </summary>
public sealed record SandboxConnectTiktokCommand(
    string MockHandle,
    int MockFollowers,
    decimal MockEngagement) : IRequest;

public sealed class SandboxConnectTiktokHandler : IRequestHandler<SandboxConnectTiktokCommand>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public SandboxConnectTiktokHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(SandboxConnectTiktokCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Influencer)
            throw new ForbiddenException("Sadece creator rolündeki kullanıcılar TikTok bağlayabilir.");

        var profile = await _db.InfluencerProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct)
            ?? throw new NotFoundException("Creator profili");

        profile.ConnectTiktok(
            cmd.MockHandle,
            $"sandbox_{user.Id}",
            cmd.MockFollowers,
            cmd.MockEngagement);

        await _db.SaveChangesAsync(ct);
    }
}
