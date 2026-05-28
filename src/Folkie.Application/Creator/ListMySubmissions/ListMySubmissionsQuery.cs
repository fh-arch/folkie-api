using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Creator.ListMySubmissions;

public sealed record ListMySubmissionsQuery(string? Status) : IRequest<List<MySubmissionDto>>;

public sealed record MySubmissionDto(
    Guid Id,
    Guid ApplicationId,
    Guid CampaignId,
    string CampaignTitle,
    string BrandName,
    string? BrandLogoUrl,
    string Status,
    string? VideoUrl,
    string? ExternalVideoUrl,
    string? Script,
    string? RevisionNote,
    string[] Hashtags,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset? PublishedAt,
    DateOnly? PublishDeadline);

public sealed class ListMySubmissionsHandler
    : IRequestHandler<ListMySubmissionsQuery, List<MySubmissionDto>>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListMySubmissionsHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<MySubmissionDto>> Handle(ListMySubmissionsQuery q, CancellationToken ct)
    {
        var user = await _currentUser.GetUserAsync(ct);
        if (user is null || user.Role != UserRole.Influencer) return new();

        var profile = await _db.InfluencerProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (profile is null) return new();

        var query =
            from s in _db.ContentSubmissions
            join a in _db.CampaignApplications on s.ApplicationId equals a.Id
            join c in _db.Campaigns on a.CampaignId equals c.Id
            join b in _db.BrandProfiles on c.BrandProfileId equals b.Id
            where a.InfluencerProfileId == profile.Id
            select new { Submission = s, Application = a, Campaign = c, Brand = b };

        if (!string.IsNullOrEmpty(q.Status)
            && Enum.TryParse<SubmissionStatus>(q.Status, ignoreCase: true, out var statusFilter))
        {
            query = query.Where(x => x.Submission.Status == statusFilter);
        }

        var rows = await query
            .OrderByDescending(x => x.Submission.SubmittedAt)
            .ToListAsync(ct);

        return rows.Select(x => new MySubmissionDto(
                x.Submission.Id,
                x.Application.Id,
                x.Campaign.Id,
                x.Campaign.Title,
                x.Brand.BrandName,
                x.Brand.LogoUrl,
                x.Submission.Status.ToString().ToLower(),
                x.Submission.VideoUrl,
                x.Submission.ExternalVideoUrl,
                x.Submission.Script,
                x.Submission.RevisionNote,
                x.Submission.Hashtags.ToArray(),
                x.Submission.SubmittedAt,
                x.Submission.ReviewedAt,
                x.Submission.PublishedAt,
                x.Campaign.PublishEndDate))
            .ToList();
    }
}
