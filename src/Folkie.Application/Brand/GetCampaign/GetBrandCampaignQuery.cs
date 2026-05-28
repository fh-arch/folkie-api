using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Brand.GetCampaign;

public sealed record GetBrandCampaignQuery(Guid CampaignId) : IRequest<BrandCampaignDetailDto>;

public sealed record BrandCampaignDetailDto(
    Guid Id,
    string Title,
    string ProductName,
    string ProductCategory,
    string Brief,
    string[] RequiredHashtags,
    string[] ContentTypes,
    string? Tone,
    string[] TargetCategories,
    string[] TargetCities,
    string[] ContentLanguage,
    int MinFollowers,
    int MaxFollowers,
    int InfluencerCount,
    decimal BudgetPerInfluencer,
    decimal TotalBudget,
    decimal PlatformFeeRate,
    string ProductDelivery,
    string ApprovalMode,
    DateOnly ApplicationDeadline,
    DateOnly PublishStartDate,
    DateOnly PublishEndDate,
    bool IsFlashCampaign,
    TimeOnly? FlashPublishTime,
    string Status,
    int ApplicationCount,
    int ApprovedCount,
    DateTimeOffset CreatedAt);

public sealed class GetBrandCampaignHandler
    : IRequestHandler<GetBrandCampaignQuery, BrandCampaignDetailDto>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetBrandCampaignHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BrandCampaignDetailDto> Handle(
        GetBrandCampaignQuery query,
        CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Brand)
            throw new ForbiddenException();

        var brand = await _db.BrandProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct)
            ?? throw new NotFoundException("Marka profili");

        var c = await _db.Campaigns
            .FirstOrDefaultAsync(x => x.Id == query.CampaignId && x.BrandProfileId == brand.Id, ct)
            ?? throw new NotFoundException("Kampanya");

        var appCount = await _db.CampaignApplications.CountAsync(a => a.CampaignId == c.Id, ct);
        var approvedCount = await _db.CampaignApplications
            .CountAsync(a => a.CampaignId == c.Id && a.Status == ApplicationStatus.Approved, ct);

        return new BrandCampaignDetailDto(
            c.Id,
            c.Title,
            c.ProductName,
            c.ProductCategory,
            c.Brief,
            c.RequiredHashtags.ToArray(),
            c.ContentTypes.Select(t => t.ToString().ToLower()).ToArray(),
            c.Tone,
            c.TargetCategories.ToArray(),
            c.TargetCities.ToArray(),
            c.ContentLanguage.ToArray(),
            c.MinFollowers,
            c.MaxFollowers,
            c.InfluencerCount,
            c.BudgetPerInfluencer,
            c.TotalBudget,
            c.PlatformFeeRate,
            c.ProductDelivery.ToString().ToLower(),
            c.ApprovalMode.ToString().ToLower(),
            c.ApplicationDeadline,
            c.PublishStartDate,
            c.PublishEndDate,
            c.IsFlashCampaign,
            c.FlashPublishTime,
            c.Status.ToString().ToLower(),
            appCount,
            approvedCount,
            c.CreatedAt);
    }
}
