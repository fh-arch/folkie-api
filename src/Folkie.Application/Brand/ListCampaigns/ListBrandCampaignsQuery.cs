using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Brand.ListCampaigns;

public sealed record ListBrandCampaignsQuery(string? Status) : IRequest<List<BrandCampaignListItemDto>>;

public sealed record BrandCampaignListItemDto(
    Guid Id,
    string Title,
    string ProductCategory,
    string Status,
    decimal Budget,
    int InfluencerCount,
    int ApplicationCount,
    int ApprovedCount,
    DateOnly ApplicationDeadline,
    bool IsFlashCampaign,
    DateTimeOffset CreatedAt);

public sealed class ListBrandCampaignsHandler
    : IRequestHandler<ListBrandCampaignsQuery, List<BrandCampaignListItemDto>>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListBrandCampaignsHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<BrandCampaignListItemDto>> Handle(
        ListBrandCampaignsQuery query,
        CancellationToken ct)
    {
        var user = await _currentUser.GetUserAsync(ct);
        if (user is null || user.Role != UserRole.Brand) return new();

        var brand = await _db.BrandProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (brand is null) return new(); // No profile yet → empty list

        var q = _db.Campaigns.Where(c => c.BrandProfileId == brand.Id);

        if (!string.IsNullOrEmpty(query.Status)
            && Enum.TryParse<CampaignStatus>(query.Status, ignoreCase: true, out var s))
        {
            q = q.Where(c => c.Status == s);
        }

        var campaigns = await q
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.ProductCategory,
                c.Status,
                c.BudgetPerInfluencer,
                c.InfluencerCount,
                c.ApplicationDeadline,
                c.IsFlashCampaign,
                c.CreatedAt,
                ApplicationCount = _db.CampaignApplications.Count(a => a.CampaignId == c.Id),
                ApprovedCount = _db.CampaignApplications.Count(a =>
                    a.CampaignId == c.Id && a.Status == ApplicationStatus.Approved),
            })
            .ToListAsync(ct);

        return campaigns.Select(c => new BrandCampaignListItemDto(
                c.Id,
                c.Title,
                c.ProductCategory,
                ToSnakeCase(c.Status.ToString()),
                c.BudgetPerInfluencer * c.InfluencerCount,
                c.InfluencerCount,
                c.ApplicationCount,
                c.ApprovedCount,
                c.ApplicationDeadline,
                c.IsFlashCampaign,
                c.CreatedAt))
            .ToList();
    }

    private static string ToSnakeCase(string s)
    {
        var result = System.Text.RegularExpressions.Regex
            .Replace(s, "([A-Z])", "_$1")
            .TrimStart('_')
            .ToLower();
        return result;
    }
}
