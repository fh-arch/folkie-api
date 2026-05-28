using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Creator.GetCampaign;

public sealed record GetCreatorCampaignQuery(Guid CampaignId) : IRequest<CreatorCampaignDetailDto>;

public sealed record CreatorCampaignDetailDto(
    Guid Id,
    Guid BrandId,
    string BrandName,
    string? BrandLogoUrl,
    string? BrandWebsite,
    string Title,
    string ProductName,
    string ProductCategory,
    string Brief,
    string[] RequiredHashtags,
    string[] ContentTypes,
    string? Tone,
    string[] TargetCities,
    string[] ContentLanguage,
    int MinFollowers,
    int MaxFollowers,
    int InfluencerCount,
    int ApprovedCount,
    decimal BudgetPerInfluencer,
    string ProductDelivery,
    DateOnly ApplicationDeadline,
    DateOnly PublishStartDate,
    DateOnly PublishEndDate,
    bool IsFlashCampaign,
    TimeOnly? FlashPublishTime,
    bool HasApplied);

public sealed class GetCreatorCampaignHandler
    : IRequestHandler<GetCreatorCampaignQuery, CreatorCampaignDetailDto>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCreatorCampaignHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CreatorCampaignDetailDto> Handle(
        GetCreatorCampaignQuery query,
        CancellationToken ct)
    {
        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == query.CampaignId, ct)
            ?? throw new NotFoundException("Kampanya");

        if (campaign.Status != CampaignStatus.Active &&
            campaign.Status != CampaignStatus.PendingPayment &&
            campaign.Status != CampaignStatus.ApplicationsClosed &&
            campaign.Status != CampaignStatus.InProgress)
        {
            throw new NotFoundException("Kampanya");
        }

        var brand = await _db.BrandProfiles
            .FirstAsync(b => b.Id == campaign.BrandProfileId, ct);

        var approvedCount = await _db.CampaignApplications
            .CountAsync(a => a.CampaignId == campaign.Id && a.Status == ApplicationStatus.Approved, ct);

        bool hasApplied = false;
        var user = await _currentUser.GetUserAsync(ct);
        if (user is not null && user.Role == UserRole.Influencer)
        {
            var profile = await _db.InfluencerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
            if (profile is not null)
            {
                hasApplied = await _db.CampaignApplications.AnyAsync(a =>
                    a.CampaignId == campaign.Id && a.InfluencerProfileId == profile.Id, ct);
            }
        }

        return new CreatorCampaignDetailDto(
            campaign.Id,
            brand.Id,
            brand.BrandName,
            brand.LogoUrl,
            brand.Website,
            campaign.Title,
            campaign.ProductName,
            campaign.ProductCategory,
            campaign.Brief,
            campaign.RequiredHashtags.ToArray(),
            campaign.ContentTypes.Select(t => t.ToString().ToLower()).ToArray(),
            campaign.Tone,
            campaign.TargetCities.ToArray(),
            campaign.ContentLanguage.ToArray(),
            campaign.MinFollowers,
            campaign.MaxFollowers,
            campaign.InfluencerCount,
            approvedCount,
            campaign.BudgetPerInfluencer,
            campaign.ProductDelivery.ToString().ToLower(),
            campaign.ApplicationDeadline,
            campaign.PublishStartDate,
            campaign.PublishEndDate,
            campaign.IsFlashCampaign,
            campaign.FlashPublishTime,
            hasApplied);
    }
}
