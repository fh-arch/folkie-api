using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Admin.ListSubmissions;

public sealed record ListSubmissionsQuery(string? Status, int Page = 1, int PageSize = 50)
    : IRequest<List<AdminSubmissionDto>>;

public sealed record AdminSubmissionDto(
    Guid Id,
    string Status,
    Guid ApplicationId,
    Guid CampaignId,
    string CampaignTitle,
    string BrandCompanyName,
    string? CreatorHandle,
    string CreatorEmail,
    string? VideoUrl,
    string? ExternalVideoUrl,
    string? RevisionNote,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset? PublishedAt);

public sealed class ListSubmissionsHandler
    : IRequestHandler<ListSubmissionsQuery, List<AdminSubmissionDto>>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListSubmissionsHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<AdminSubmissionDto>> Handle(
        ListSubmissionsQuery q, CancellationToken ct)
    {
        await EnsureAdmin(ct);

        var query = _db.ContentSubmissions.AsQueryable();
        if (!string.IsNullOrEmpty(q.Status))
        {
            query = query.Where(s => s.Status.ToString().ToLower() == q.Status.ToLower());
        }

        var rows = await query
            .OrderByDescending(s => s.SubmittedAt)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(s => new
            {
                s.Id,
                Status = s.Status.ToString().ToLower(),
                s.ApplicationId,
                s.VideoUrl,
                s.ExternalVideoUrl,
                s.RevisionNote,
                s.SubmittedAt,
                s.ReviewedAt,
                s.PublishedAt,
                Application = _db.CampaignApplications
                    .Where(a => a.Id == s.ApplicationId)
                    .Select(a => new { a.CampaignId, a.InfluencerProfileId })
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        var campaignIds = rows.Where(r => r.Application != null)
            .Select(r => r.Application!.CampaignId)
            .Distinct().ToList();
        var campaigns = await _db.Campaigns
            .Where(c => campaignIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Title, c.BrandProfileId })
            .ToDictionaryAsync(c => c.Id, ct);

        var brandIds = campaigns.Values.Select(c => c.BrandProfileId).Distinct().ToList();
        var brands = await _db.BrandProfiles
            .Where(b => brandIds.Contains(b.Id))
            .Select(b => new { b.Id, b.BrandName })
            .ToDictionaryAsync(b => b.Id, b => b.BrandName, ct);

        var influencerIds = rows.Where(r => r.Application != null)
            .Select(r => r.Application!.InfluencerProfileId)
            .Distinct().ToList();
        var influencers = await _db.InfluencerProfiles
            .Where(i => influencerIds.Contains(i.Id))
            .Select(i => new { i.Id, i.TiktokHandle, i.UserId })
            .ToDictionaryAsync(i => i.Id, ct);

        var userIds = influencers.Values.Select(i => i.UserId).Distinct().ToList();
        var emails = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToDictionaryAsync(u => u.Id, u => u.Email, ct);

        return rows.Select(r =>
        {
            string brandName = "";
            string handle = "";
            string email = "";
            Guid campaignId = Guid.Empty;
            string campaignTitle = "(Başvuru silinmiş)";
            if (r.Application != null)
            {
                campaignId = r.Application.CampaignId;
                if (campaigns.TryGetValue(r.Application.CampaignId, out var c))
                {
                    campaignTitle = c.Title;
                    if (brands.TryGetValue(c.BrandProfileId, out var b)) brandName = b;
                }
                if (influencers.TryGetValue(r.Application.InfluencerProfileId, out var inf))
                {
                    handle = inf.TiktokHandle ?? "";
                    if (emails.TryGetValue(inf.UserId, out var em)) email = em;
                }
            }
            return new AdminSubmissionDto(
                r.Id,
                r.Status,
                r.ApplicationId,
                campaignId,
                campaignTitle,
                brandName,
                handle,
                email,
                r.VideoUrl,
                r.ExternalVideoUrl,
                r.RevisionNote,
                r.SubmittedAt,
                r.ReviewedAt,
                r.PublishedAt);
        }).ToList();
    }

    private async Task EnsureAdmin(CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Admin)
            throw new ForbiddenException("Admin yetkisi gerekli.");
    }
}
