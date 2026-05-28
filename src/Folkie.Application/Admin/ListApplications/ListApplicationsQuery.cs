using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Admin.ListApplications;

public sealed record ListApplicationsQuery(string? Status, int Page = 1, int PageSize = 50)
    : IRequest<List<AdminApplicationDto>>;

public sealed record AdminApplicationDto(
    Guid Id,
    string Status,
    Guid CampaignId,
    string CampaignTitle,
    string BrandCompanyName,
    Guid InfluencerProfileId,
    string? CreatorHandle,
    string CreatorEmail,
    int FollowerCount,
    decimal Amount,
    string? RejectionReason,
    DateTimeOffset AppliedAt,
    DateTimeOffset? ReviewedAt);

public sealed class ListApplicationsHandler
    : IRequestHandler<ListApplicationsQuery, List<AdminApplicationDto>>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListApplicationsHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<AdminApplicationDto>> Handle(
        ListApplicationsQuery q, CancellationToken ct)
    {
        await EnsureAdmin(ct);

        var query = _db.CampaignApplications.AsQueryable();
        if (!string.IsNullOrEmpty(q.Status))
        {
            query = query.Where(a => a.Status.ToString().ToLower() == q.Status.ToLower());
        }

        var rows = await query
            .OrderByDescending(a => a.AppliedAt)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(a => new
            {
                a.Id,
                Status = a.Status.ToString().ToLower(),
                a.CampaignId,
                a.InfluencerProfileId,
                a.RejectionReason,
                a.AppliedAt,
                a.ReviewedAt,
                Campaign = _db.Campaigns
                    .Where(c => c.Id == a.CampaignId)
                    .Select(c => new
                    {
                        c.Title,
                        c.BudgetPerInfluencer,
                        c.BrandProfileId,
                    })
                    .FirstOrDefault(),
                Influencer = _db.InfluencerProfiles
                    .Where(i => i.Id == a.InfluencerProfileId)
                    .Select(i => new
                    {
                        i.TiktokHandle,
                        i.UserId,
                        i.FollowerCount,
                    })
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        var brandIds = rows
            .Where(r => r.Campaign != null)
            .Select(r => r.Campaign!.BrandProfileId)
            .Distinct()
            .ToList();
        var brands = await _db.BrandProfiles
            .Where(b => brandIds.Contains(b.Id))
            .Select(b => new { b.Id, b.BrandName })
            .ToDictionaryAsync(b => b.Id, b => b.BrandName, ct);

        var userIds = rows
            .Where(r => r.Influencer != null)
            .Select(r => r.Influencer!.UserId)
            .Distinct()
            .ToList();
        var emails = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToDictionaryAsync(u => u.Id, u => u.Email, ct);

        return rows.Select(r => new AdminApplicationDto(
                r.Id,
                r.Status,
                r.CampaignId,
                r.Campaign?.Title ?? "(Kampanya silinmiş)",
                r.Campaign != null && brands.TryGetValue(r.Campaign.BrandProfileId, out var brand)
                    ? brand
                    : "",
                r.InfluencerProfileId,
                r.Influencer?.TiktokHandle,
                r.Influencer != null && emails.TryGetValue(r.Influencer.UserId, out var email)
                    ? email
                    : "",
                r.Influencer?.FollowerCount ?? 0,
                r.Campaign?.BudgetPerInfluencer ?? 0,
                r.RejectionReason,
                r.AppliedAt,
                r.ReviewedAt))
            .ToList();
    }

    private async Task EnsureAdmin(CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Admin)
            throw new ForbiddenException("Admin yetkisi gerekli.");
    }
}
