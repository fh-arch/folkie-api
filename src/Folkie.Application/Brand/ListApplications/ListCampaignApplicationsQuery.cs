using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Brand.ListApplications;

public sealed record ListCampaignApplicationsQuery(Guid CampaignId, string? Status)
    : IRequest<List<CampaignApplicationDto>>;

public sealed record CampaignApplicationDto(
    Guid Id,
    Guid CreatorProfileId,
    string? Handle,
    int FollowerCount,
    decimal EngagementRate,
    string Tier,
    string? City,
    string[] Categories,
    string Status,
    string? RejectionReason,
    DateTimeOffset AppliedAt,
    DateTimeOffset? ReviewedAt);

public sealed class ListCampaignApplicationsHandler
    : IRequestHandler<ListCampaignApplicationsQuery, List<CampaignApplicationDto>>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListCampaignApplicationsHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<CampaignApplicationDto>> Handle(
        ListCampaignApplicationsQuery query,
        CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Brand)
            throw new ForbiddenException();

        var brand = await _db.BrandProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct)
            ?? throw new NotFoundException("Marka profili");

        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == query.CampaignId && c.BrandProfileId == brand.Id, ct)
            ?? throw new NotFoundException("Kampanya");

        var q = _db.CampaignApplications.Where(a => a.CampaignId == campaign.Id);

        if (!string.IsNullOrEmpty(query.Status)
            && Enum.TryParse<ApplicationStatus>(query.Status, ignoreCase: true, out var s))
        {
            q = q.Where(a => a.Status == s);
        }

        var rows = await q
            .OrderByDescending(a => a.AppliedAt)
            .Join(_db.InfluencerProfiles,
                a => a.InfluencerProfileId,
                p => p.Id,
                (a, p) => new
                {
                    a.Id,
                    CreatorProfileId = p.Id,
                    p.TiktokHandle,
                    p.FollowerCount,
                    p.EngagementRate,
                    p.City,
                    p.Categories,
                    a.Status,
                    a.RejectionReason,
                    a.AppliedAt,
                    a.ReviewedAt,
                })
            .ToListAsync(ct);

        return rows.Select(r => new CampaignApplicationDto(
                r.Id,
                r.CreatorProfileId,
                r.TiktokHandle,
                r.FollowerCount,
                r.EngagementRate,
                TierFor(r.FollowerCount),
                r.City,
                r.Categories.ToArray(),
                r.Status.ToString().ToLower(),
                r.RejectionReason,
                r.AppliedAt,
                r.ReviewedAt))
            .ToList();
    }

    private static string TierFor(int followers) => followers switch
    {
        < 10_000 => "nano",
        < 100_000 => "micro",
        _ => "mid_tier",
    };
}
