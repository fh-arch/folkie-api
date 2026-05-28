using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Admin.GetUserDetail;

public sealed record GetUserDetailQuery(Guid UserId) : IRequest<AdminUserDetailDto>;

public sealed record AdminUserDetailDto(
    Guid Id,
    string ClerkUserId,
    string Email,
    string Role,
    string? FullName,
    string? AvatarUrl,
    DateTimeOffset CreatedAt,
    AdminBrandSection? Brand,
    AdminInfluencerSection? Influencer);

public sealed record AdminBrandSection(
    Guid Id,
    string CompanyName,
    string? Industry,
    string? Website,
    string? TaxId,
    string? ContactName,
    string? ContactPhone,
    string? BillingAddress,
    int CampaignCount,
    int ActiveCampaignCount,
    decimal TotalSpent);

public sealed record AdminInfluencerSection(
    Guid Id,
    string? TiktokHandle,
    string? TiktokUserId,
    int FollowerCount,
    decimal EngagementRate,
    decimal FakeFollowerScore,
    string? City,
    string? Country,
    string? Bio,
    string Tier,
    List<string> Categories,
    List<string> Subcategories,
    List<string> ContentLanguage,
    bool HasIban,
    string? IbanName,
    int ApplicationCount,
    int ApprovedApplicationCount,
    decimal TotalEarned);

public sealed class GetUserDetailHandler
    : IRequestHandler<GetUserDetailQuery, AdminUserDetailDto>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetUserDetailHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AdminUserDetailDto> Handle(
        GetUserDetailQuery q, CancellationToken ct)
    {
        await EnsureAdmin(ct);

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == q.UserId, ct)
            ?? throw new NotFoundException("Kullanıcı bulunamadı");

        AdminBrandSection? brandSection = null;
        AdminInfluencerSection? influencerSection = null;

        if (user.Role == UserRole.Brand)
        {
            var b = await _db.BrandProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
            if (b != null)
            {
                var allCampaigns = await _db.Campaigns
                    .Where(c => c.BrandProfileId == b.Id)
                    .Select(c => new { c.Status, Total = c.InfluencerCount * c.BudgetPerInfluencer })
                    .ToListAsync(ct);
                brandSection = new AdminBrandSection(
                    b.Id,
                    b.BrandName,
                    b.Industry,
                    b.Website,
                    b.TaxId,
                    b.ContactName,
                    b.ContactPhone,
                    b.BillingAddress,
                    allCampaigns.Count,
                    allCampaigns.Count(c =>
                        c.Status == CampaignStatus.Active
                        || c.Status == CampaignStatus.InProgress),
                    allCampaigns.Sum(c => c.Total));
            }
        }

        if (user.Role == UserRole.Influencer)
        {
            var i = await _db.InfluencerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
            if (i != null)
            {
                var applications = await _db.CampaignApplications
                    .Where(a => a.InfluencerProfileId == i.Id)
                    .Select(a => new { a.Status, a.CampaignId })
                    .ToListAsync(ct);
                var approvedCampaignIds = applications
                    .Where(a => a.Status == ApplicationStatus.Approved)
                    .Select(a => a.CampaignId)
                    .ToList();
                var earned = await _db.Campaigns
                    .Where(c => approvedCampaignIds.Contains(c.Id))
                    .SumAsync(c => c.BudgetPerInfluencer, ct);

                influencerSection = new AdminInfluencerSection(
                    i.Id,
                    i.TiktokHandle,
                    i.TiktokUserId,
                    i.FollowerCount,
                    i.EngagementRate,
                    i.FakeFollowerScore ?? 0m,
                    i.City,
                    i.Country,
                    i.Bio,
                    i.Tier.ToString().ToLower(),
                    i.Categories ?? new List<string>(),
                    i.Subcategories ?? new List<string>(),
                    i.ContentLanguage ?? new List<string>(),
                    i.Iban != null && !string.IsNullOrEmpty(i.Iban.Cipher),
                    i.IbanName,
                    applications.Count,
                    applications.Count(a => a.Status == ApplicationStatus.Approved),
                    earned);
            }
        }

        return new AdminUserDetailDto(
            user.Id,
            user.ClerkUserId,
            user.Email,
            user.Role.ToString().ToLower(),
            user.FullName,
            user.AvatarUrl,
            user.CreatedAt,
            brandSection,
            influencerSection);
    }

    private async Task EnsureAdmin(CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Admin)
            throw new ForbiddenException("Admin yetkisi gerekli.");
    }
}
