using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Admin.ListCreatorsForApproval;

public sealed record ListCreatorsForApprovalQuery(string? Filter = "pending")
    : IRequest<List<AdminCreatorRowDto>>;

public sealed record AdminCreatorRowDto(
    Guid UserId,
    Guid InfluencerProfileId,
    string Email,
    string? FullName,
    string? TiktokHandle,
    int FollowerCount,
    decimal EngagementRate,
    string Tier,
    string? City,
    List<string> Categories,
    bool HasIban,
    bool IsVerified,
    bool IsActive,
    int ApplicationCount,
    int ApprovedApplicationCount,
    DateTimeOffset CreatedAt);

public sealed class ListCreatorsForApprovalHandler
    : IRequestHandler<ListCreatorsForApprovalQuery, List<AdminCreatorRowDto>>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListCreatorsForApprovalHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<AdminCreatorRowDto>> Handle(
        ListCreatorsForApprovalQuery q, CancellationToken ct)
    {
        await EnsureAdmin(ct);

        var query = (
            from i in _db.InfluencerProfiles
            join u in _db.Users on i.UserId equals u.Id
            select new { i, u }
        );

        query = q.Filter switch
        {
            "pending" => query.Where(x => !x.i.IsVerified && x.i.IsActive),
            "verified" => query.Where(x => x.i.IsVerified && x.i.IsActive),
            "suspended" => query.Where(x => !x.i.IsActive),
            _ => query,
        };

        var rows = await query
            .OrderBy(x => x.i.CreatedAt)
            .Select(x => new
            {
                x.u.Id,
                InfluencerId = x.i.Id,
                x.u.Email,
                x.u.FullName,
                x.i.TiktokHandle,
                x.i.FollowerCount,
                x.i.EngagementRate,
                x.i.City,
                x.i.Categories,
                HasIban = x.i.Iban != null && x.i.Iban.Cipher != "",
                x.i.IsVerified,
                x.i.IsActive,
                ApplicationCount = _db.CampaignApplications.Count(
                    a => a.InfluencerProfileId == x.i.Id),
                ApprovedApplicationCount = _db.CampaignApplications.Count(
                    a => a.InfluencerProfileId == x.i.Id
                        && a.Status == ApplicationStatus.Approved),
                x.u.CreatedAt,
            })
            .ToListAsync(ct);

        return rows.Select(r =>
        {
            // Compute tier from follower count (matches InfluencerProfile.Tier logic)
            var tier = r.FollowerCount switch
            {
                < 1_000 => "micro",
                < 10_000 => "nano",
                < 100_000 => "mid_tier",
                _ => "macro",
            };

            return new AdminCreatorRowDto(
                r.Id,
                r.InfluencerId,
                r.Email,
                r.FullName,
                r.TiktokHandle,
                r.FollowerCount,
                r.EngagementRate,
                tier,
                r.City,
                r.Categories ?? new List<string>(),
                r.HasIban,
                r.IsVerified,
                r.IsActive,
                r.ApplicationCount,
                r.ApprovedApplicationCount,
                r.CreatedAt);
        }).ToList();
    }

    private async Task EnsureAdmin(CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Admin)
            throw new ForbiddenException("Admin yetkisi gerekli.");
    }
}
