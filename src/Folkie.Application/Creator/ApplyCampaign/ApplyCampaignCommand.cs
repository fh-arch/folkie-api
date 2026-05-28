using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Applications;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Creator.ApplyCampaign;

public sealed record ApplyCampaignCommand(Guid CampaignId) : IRequest<Guid>;

public sealed class ApplyCampaignHandler : IRequestHandler<ApplyCampaignCommand, Guid>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;

    public ApplyCampaignHandler(
        IFolkieDbContext db,
        ICurrentUser currentUser,
        INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<Guid> Handle(ApplyCampaignCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Influencer)
            throw new ForbiddenException("Sadece creator'lar başvuru yapabilir.");

        var profile = await _db.InfluencerProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct)
            ?? throw new NotFoundException("Creator profili. Önce profilini tamamla.");

        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == cmd.CampaignId, ct)
            ?? throw new NotFoundException("Kampanya");

        if (campaign.Status != CampaignStatus.Active && campaign.Status != CampaignStatus.PendingPayment)
            throw new ConflictException("Kampanya başvurulara açık değil.");

        if (profile.FollowerCount < campaign.MinFollowers
            || profile.FollowerCount > campaign.MaxFollowers)
            throw new ConflictException(
                $"Takipçi sayın bu kampanya için uygun değil ({campaign.MinFollowers}-{campaign.MaxFollowers} arası bekleniyor).");

        var existing = await _db.CampaignApplications.AnyAsync(a =>
            a.CampaignId == campaign.Id && a.InfluencerProfileId == profile.Id, ct);
        if (existing)
            throw new ConflictException("Bu kampanyaya zaten başvurmuşsun.");

        var application = CampaignApplication.Create(campaign.Id, profile.Id);
        _db.CampaignApplications.Add(application);
        await _db.SaveChangesAsync(ct);

        // Notify brand owner
        var brand = await _db.BrandProfiles.FirstAsync(b => b.Id == campaign.BrandProfileId, ct);
        await _notifications.NotifyAsync(
            brand.UserId,
            "application.created",
            "Yeni başvuru!",
            $"{profile.TiktokHandle ?? "Bir creator"} \"{campaign.Title}\" kampanyana başvurdu.",
            new { campaignId = campaign.Id, applicationId = application.Id },
            ct);

        return application.Id;
    }
}
