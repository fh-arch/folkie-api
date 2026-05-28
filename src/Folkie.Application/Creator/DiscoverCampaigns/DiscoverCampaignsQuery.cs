using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Creator.DiscoverCampaigns;

public sealed record DiscoverCampaignsQuery(
    string? Category,
    string? City,
    int? MaxFollowers,
    bool NanoOnly,
    int Page = 1,
    int PageSize = 20) : IRequest<DiscoverCampaignsResult>;

public sealed record DiscoverCampaignCardDto(
    Guid Id,
    Guid BrandId,
    string BrandName,
    string? BrandLogoUrl,
    string Title,
    string ProductCategory,
    decimal BudgetPerInfluencer,
    int InfluencerCount,
    int ApprovedCount,
    DateOnly ApplicationDeadline,
    bool IsFlashCampaign,
    bool IsNanoOnly,
    int MatchScore);

public sealed record DiscoverCampaignsResult(
    List<DiscoverCampaignCardDto> Items,
    int Total,
    int Page,
    int PageSize);

public sealed class DiscoverCampaignsHandler
    : IRequestHandler<DiscoverCampaignsQuery, DiscoverCampaignsResult>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DiscoverCampaignsHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DiscoverCampaignsResult> Handle(
        DiscoverCampaignsQuery query,
        CancellationToken ct)
    {
        // Creator profili varsa eşleşme skoru hesaplanır; yoksa default 50
        var user = await _currentUser.GetUserAsync(ct);
        var creator = user is null ? null : await _db.InfluencerProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);

        var q = _db.Campaigns.Where(c =>
            c.Status == CampaignStatus.Active ||
            c.Status == CampaignStatus.PendingPayment);

        if (!string.IsNullOrEmpty(query.Category))
            q = q.Where(c => c.TargetCategories.Contains(query.Category));

        if (!string.IsNullOrEmpty(query.City))
            q = q.Where(c => c.TargetCities.Count == 0 || c.TargetCities.Contains(query.City));

        if (query.NanoOnly)
            q = q.Where(c => c.MaxFollowers <= 10_000);

        var total = await q.CountAsync(ct);

        var rows = await q
            .OrderByDescending(c => c.IsFlashCampaign)
            .ThenBy(c => c.ApplicationDeadline)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new
            {
                Campaign = c,
                BrandName = _db.BrandProfiles
                    .Where(b => b.Id == c.BrandProfileId)
                    .Select(b => b.BrandName)
                    .FirstOrDefault() ?? "",
                BrandLogo = _db.BrandProfiles
                    .Where(b => b.Id == c.BrandProfileId)
                    .Select(b => b.LogoUrl)
                    .FirstOrDefault(),
                ApprovedCount = _db.CampaignApplications.Count(a =>
                    a.CampaignId == c.Id && a.Status == ApplicationStatus.Approved),
            })
            .ToListAsync(ct);

        var items = rows.Select(r =>
        {
            var c = r.Campaign;
            int matchScore = creator is null
                ? 50
                : CalculateMatchScore(creator, c);

            return new DiscoverCampaignCardDto(
                c.Id,
                c.BrandProfileId,
                r.BrandName,
                r.BrandLogo,
                c.Title,
                c.ProductCategory,
                c.BudgetPerInfluencer,
                c.InfluencerCount,
                r.ApprovedCount,
                c.ApplicationDeadline,
                c.IsFlashCampaign,
                c.MaxFollowers <= 10_000,
                matchScore);
        }).ToList();

        return new DiscoverCampaignsResult(items, total, query.Page, query.PageSize);
    }

    /// <summary>
    /// Çok faktörlü ağırlıklı skor (max 100). Detaylı plan: docs/AI_MATCHING.md
    ///
    /// Faz 1 (şu an): rule-based, açıklanabilir.
    /// Faz 2 (Sprint 5): + pgvector semantic similarity (creator bio vs brief).
    /// Faz 3: + GPT-4o-mini scorer + TikTok content analizi.
    /// </summary>
    private static int CalculateMatchScore(
        Domain.Influencers.InfluencerProfile creator,
        Domain.Campaigns.Campaign campaign)
    {
        // 1) Kategori uyumu — overlap ne kadar yüksekse o kadar puan (max 30)
        int categoryScore = 0;
        if (campaign.TargetCategories.Count == 0)
        {
            categoryScore = 15; // Hedef yoksa orta puan
        }
        else
        {
            int overlap = creator.Categories
                .Count(cc => campaign.TargetCategories.Contains(cc));
            categoryScore = overlap switch
            {
                0 => 0,
                1 => 20,
                2 => 27,
                _ => 30,
            };
        }

        // 2) Şehir / coğrafya (max 15)
        int geoScore;
        if (campaign.TargetCities.Count == 0)
            geoScore = 10; // Tüm Türkiye'ye açık
        else if (creator.City is not null && campaign.TargetCities.Contains(creator.City))
            geoScore = 15;
        else
            geoScore = 0;

        // 3) Takipçi aralığı (max 25) — soft penalty just outside range
        int followerScore;
        if (creator.FollowerCount >= campaign.MinFollowers
            && creator.FollowerCount <= campaign.MaxFollowers)
        {
            followerScore = 25;
        }
        else if (creator.FollowerCount >= campaign.MinFollowers * 0.8
                 && creator.FollowerCount <= campaign.MaxFollowers * 1.2)
        {
            followerScore = 12; // Sınırın hemen dışında, yine de uygun olabilir
        }
        else
        {
            followerScore = 0;
        }

        // 4) Etkileşim oranı (max 15) — yüksek engagement = gerçek kitle
        int engagementScore = creator.EngagementRate switch
        {
            >= 7m => 15,
            >= 5m => 12,
            >= 3m => 8,
            >= 1m => 4,
            _ => 0,
        };

        // 5) İçerik dili overlap (max 5)
        int languageScore = creator.ContentLanguage
            .Any(l => campaign.ContentLanguage.Contains(l)) ? 5 : 0;

        // 6) Doğrulanmış creator bonusu (max 10) — fake_follower_score kontrolü dahil
        int trustScore = 0;
        if (creator.IsVerified) trustScore += 5;
        if (creator.FakeFollowerScore is not null && creator.FakeFollowerScore < 30m)
            trustScore += 5;
        else if (creator.FakeFollowerScore is null)
            trustScore += 2; // Henüz analiz edilmedi, ihtiyatlı puan

        var total = categoryScore + geoScore + followerScore
                    + engagementScore + languageScore + trustScore;

        return Math.Clamp(total, 0, 100);
    }
}
