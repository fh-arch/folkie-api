using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Brand.ApproveApplication;

public sealed record ApproveApplicationCommand(Guid ApplicationId) : IRequest<Unit>;

public sealed class ApproveApplicationHandler : IRequestHandler<ApproveApplicationCommand, Unit>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;

    public ApproveApplicationHandler(
        IFolkieDbContext db,
        ICurrentUser currentUser,
        INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<Unit> Handle(ApproveApplicationCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Brand) throw new ForbiddenException();

        var brand = await _db.BrandProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct)
            ?? throw new NotFoundException("Marka profili");

        var application = await _db.CampaignApplications
            .FirstOrDefaultAsync(a => a.Id == cmd.ApplicationId, ct)
            ?? throw new NotFoundException("Başvuru");

        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == application.CampaignId, ct)
            ?? throw new NotFoundException("Kampanya");

        if (campaign.BrandProfileId != brand.Id)
            throw new ForbiddenException("Bu başvuruyu onaylama yetkin yok.");

        application.Approve();
        await _db.SaveChangesAsync(ct);

        // Notify creator
        var creator = await _db.InfluencerProfiles.FirstAsync(p => p.Id == application.InfluencerProfileId, ct);
        await _notifications.NotifyAsync(
            creator.UserId,
            "application.approved",
            "Başvurun onaylandı! 🎉",
            $"\"{campaign.Title}\" kampanyasına kabul edildin. İçerik yüklemeye hazır.",
            new { campaignId = campaign.Id, applicationId = application.Id },
            ct);

        return Unit.Value;
    }
}
