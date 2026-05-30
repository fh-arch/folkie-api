using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Creator.ListApplications;

public sealed record ListMyApplicationsQuery(string? Status) : IRequest<List<MyApplicationDto>>;

public sealed record MyApplicationDto(
    Guid Id,
    Guid CampaignId,
    string CampaignTitle,
    Guid BrandId,
    string BrandName,
    string? BrandLogoUrl,
    decimal Amount,
    string Status,
    string? RejectionReason,
    DateTimeOffset AppliedAt,
    DateTimeOffset? ReviewedAt,
    DateOnly PublishEndDate);

public sealed class ListMyApplicationsHandler
    : IRequestHandler<ListMyApplicationsQuery, List<MyApplicationDto>>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListMyApplicationsHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<MyApplicationDto>> Handle(
        ListMyApplicationsQuery query,
        CancellationToken ct)
    {
        var user = await _currentUser.GetUserAsync(ct);
        if (user is null || user.Role != UserRole.Influencer) return new();

        var profile = await _db.InfluencerProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (profile is null) return new();

        var q = _db.CampaignApplications.Where(a => a.InfluencerProfileId == profile.Id);

        if (!string.IsNullOrEmpty(query.Status)
            && Enum.TryParse<ApplicationStatus>(query.Status, ignoreCase: true, out var s))
        {
            q = q.Where(a => a.Status == s);
        }

        return await q
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new MyApplicationDto(
                a.Id,
                a.CampaignId,
                _db.Campaigns.Where(c => c.Id == a.CampaignId).Select(c => c.Title).FirstOrDefault() ?? "",
                _db.Campaigns.Where(c => c.Id == a.CampaignId).Select(c => c.BrandProfileId).FirstOrDefault(),
                _db.Campaigns.Where(c => c.Id == a.CampaignId)
                    .Join(_db.BrandProfiles, c => c.BrandProfileId, b => b.Id, (c, b) => b.BrandName)
                    .FirstOrDefault() ?? "",
                _db.Campaigns.Where(c => c.Id == a.CampaignId)
                    .Join(_db.BrandProfiles, c => c.BrandProfileId, b => b.Id, (c, b) => b.LogoUrl)
                    .FirstOrDefault(),
                _db.Campaigns.Where(c => c.Id == a.CampaignId).Select(c => c.BudgetPerInfluencer).FirstOrDefault(),
                a.Status.ToString().ToLower(),
                a.RejectionReason,
                a.AppliedAt,
                a.ReviewedAt,
                _db.Campaigns.Where(c => c.Id == a.CampaignId).Select(c => c.PublishEndDate).FirstOrDefault()))
            .ToListAsync(ct);
    }
}
