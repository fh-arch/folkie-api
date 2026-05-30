using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Brand.ListCampaignSubmissions;

public sealed record ListCampaignSubmissionsQuery(Guid CampaignId) : IRequest<List<CampaignSubmissionDto>>;

public sealed record CampaignSubmissionDto(
    Guid Id,
    Guid ApplicationId,
    Guid CreatorProfileId,
    string? Handle,
    string? AvatarUrl,
    int FollowerCount,
    decimal EngagementRate,
    string SubmissionStatus,
    string? VideoUrl,
    string? ExternalVideoUrl,
    string? Script,
    string? RevisionNote,
    string[] Hashtags,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ReviewedAt);

public sealed class ListCampaignSubmissionsHandler
    : IRequestHandler<ListCampaignSubmissionsQuery, List<CampaignSubmissionDto>>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListCampaignSubmissionsHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<CampaignSubmissionDto>> Handle(
        ListCampaignSubmissionsQuery q, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Brand) throw new ForbiddenException();

        var brand = await _db.BrandProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct)
            ?? throw new NotFoundException("Brand profile");

        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == q.CampaignId && c.BrandProfileId == brand.Id, ct)
            ?? throw new NotFoundException("Campaign");

        var rows = await (
            from s in _db.ContentSubmissions
            join a in _db.CampaignApplications on s.ApplicationId equals a.Id
            join p in _db.InfluencerProfiles on a.InfluencerProfileId equals p.Id
            where a.CampaignId == campaign.Id
            orderby s.SubmittedAt descending
            select new CampaignSubmissionDto(
                s.Id,
                a.Id,
                p.Id,
                p.TiktokHandle,
                p.TiktokAvatarUrl,
                p.FollowerCount,
                p.EngagementRate,
                s.Status.ToString().ToLower(),
                s.VideoUrl,
                s.ExternalVideoUrl,
                s.Script,
                s.RevisionNote,
                s.Hashtags.ToArray(),
                s.SubmittedAt,
                s.ReviewedAt)
        ).ToListAsync(ct);

        return rows;
    }
}
