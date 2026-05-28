using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Admin.ListCampaigns;

public sealed record ListCampaignsQuery(string? Status, int Page = 1, int PageSize = 50)
    : IRequest<List<AdminCampaignDto>>;

public sealed record AdminCampaignDto(
    Guid Id,
    string Title,
    string ProductCategory,
    string Status,
    Guid BrandProfileId,
    string BrandCompanyName,
    string BrandEmail,
    int InfluencerCount,
    decimal BudgetPerInfluencer,
    decimal TotalBudget,
    int ApplicationCount,
    int ApprovedCount,
    bool IsFlashCampaign,
    DateOnly ApplicationDeadline,
    DateTimeOffset CreatedAt);

public sealed class ListCampaignsHandler
    : IRequestHandler<ListCampaignsQuery, List<AdminCampaignDto>>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListCampaignsHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<AdminCampaignDto>> Handle(ListCampaignsQuery q, CancellationToken ct)
    {
        await EnsureAdmin(ct);

        var query = _db.Campaigns.AsQueryable();
        if (!string.IsNullOrEmpty(q.Status))
        {
            query = query.Where(c => c.Status.ToString().ToLower() == q.Status.ToLower());
        }

        var rows = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.ProductCategory,
                Status = c.Status.ToString().ToLower(),
                c.BrandProfileId,
                BrandProfile = _db.BrandProfiles
                    .Where(b => b.Id == c.BrandProfileId)
                    .Select(b => new
                    {
                        b.BrandName,
                        b.UserId,
                    })
                    .FirstOrDefault(),
                c.InfluencerCount,
                c.BudgetPerInfluencer,
                ApplicationCount = _db.CampaignApplications.Count(a => a.CampaignId == c.Id),
                ApprovedCount = _db.CampaignApplications.Count(a =>
                    a.CampaignId == c.Id
                    && a.Status == Folkie.Domain.Common.ApplicationStatus.Approved),
                c.IsFlashCampaign,
                c.ApplicationDeadline,
                c.CreatedAt,
            })
            .ToListAsync(ct);

        // Resolve brand emails via Users join (one extra round-trip; cheap)
        var userIds = rows
            .Where(r => r.BrandProfile != null)
            .Select(r => r.BrandProfile!.UserId)
            .Distinct()
            .ToList();
        var emails = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToDictionaryAsync(u => u.Id, u => u.Email, ct);

        return rows.Select(r => new AdminCampaignDto(
                r.Id,
                r.Title,
                r.ProductCategory,
                r.Status,
                r.BrandProfileId,
                r.BrandProfile?.BrandName ?? "(Marka bulunamadı)",
                r.BrandProfile != null && emails.TryGetValue(r.BrandProfile.UserId, out var email)
                    ? email
                    : "",
                r.InfluencerCount,
                r.BudgetPerInfluencer,
                r.InfluencerCount * r.BudgetPerInfluencer,
                r.ApplicationCount,
                r.ApprovedCount,
                r.IsFlashCampaign,
                r.ApplicationDeadline,
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
