using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Admin.ListBrandsForApproval;

public sealed record ListBrandsForApprovalQuery(string? Filter = "pending")
    : IRequest<List<AdminBrandRowDto>>;

public sealed record AdminBrandRowDto(
    Guid UserId,
    Guid BrandProfileId,
    string Email,
    string? FullName,
    string BrandName,
    string? Industry,
    string? Website,
    string? TaxId,
    string? ContactName,
    string? ContactPhone,
    bool IsVerified,
    bool IsActive,
    int CampaignCount,
    decimal TotalSpent,
    DateTimeOffset CreatedAt);

public sealed class ListBrandsForApprovalHandler
    : IRequestHandler<ListBrandsForApprovalQuery, List<AdminBrandRowDto>>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListBrandsForApprovalHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<AdminBrandRowDto>> Handle(
        ListBrandsForApprovalQuery q, CancellationToken ct)
    {
        await EnsureAdmin(ct);

        var query = (
            from b in _db.BrandProfiles
            join u in _db.Users on b.UserId equals u.Id
            select new { b, u }
        );

        query = q.Filter switch
        {
            "pending" => query.Where(x => !x.b.IsVerified && x.b.IsActive),
            "verified" => query.Where(x => x.b.IsVerified && x.b.IsActive),
            "suspended" => query.Where(x => !x.b.IsActive),
            _ => query,
        };

        var rows = await query
            .OrderBy(x => x.b.CreatedAt)
            .Select(x => new
            {
                x.u.Id,
                BrandId = x.b.Id,
                x.u.Email,
                x.u.FullName,
                x.b.BrandName,
                x.b.Industry,
                x.b.Website,
                x.b.TaxId,
                x.b.ContactName,
                x.b.ContactPhone,
                x.b.IsVerified,
                x.b.IsActive,
                CampaignCount = _db.Campaigns.Count(c => c.BrandProfileId == x.b.Id),
                TotalSpent = _db.Campaigns
                    .Where(c => c.BrandProfileId == x.b.Id)
                    .Sum(c => (decimal?)(c.InfluencerCount * c.BudgetPerInfluencer)) ?? 0m,
                x.u.CreatedAt,
            })
            .ToListAsync(ct);

        return rows.Select(r => new AdminBrandRowDto(
                r.Id,
                r.BrandId,
                r.Email,
                r.FullName,
                r.BrandName,
                r.Industry,
                r.Website,
                r.TaxId,
                r.ContactName,
                r.ContactPhone,
                r.IsVerified,
                r.IsActive,
                r.CampaignCount,
                r.TotalSpent,
                r.CreatedAt))
            .ToList();
    }

    private async Task EnsureAdmin(CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Admin)
            throw new ForbiddenException("Admin yetkisi gerekli.");
    }
}
