using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Brand.SearchCreators;

public sealed record SearchCreatorsQuery(
    string? Q,
    string? Category,
    string? City,
    int? MinFollowers,
    int? MaxFollowers,
    decimal? MinEngagement,
    string? Tier,        // nano | micro | mid_tier | null = all
    bool VerifiedOnly,
    int Page = 1,
    int PageSize = 20) : IRequest<SearchCreatorsResult>;

public sealed record CreatorCardDto(
    Guid Id,
    string? Handle,
    int FollowerCount,
    decimal EngagementRate,
    string Tier,
    string? City,
    string[] Categories,
    string? Bio,
    bool IsVerified,
    bool IsFavorited);

public sealed record SearchCreatorsResult(
    List<CreatorCardDto> Items,
    int Total,
    int Page,
    int PageSize);

public sealed class SearchCreatorsHandler
    : IRequestHandler<SearchCreatorsQuery, SearchCreatorsResult>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public SearchCreatorsHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SearchCreatorsResult> Handle(
        SearchCreatorsQuery q,
        CancellationToken ct)
    {
        var user = await _currentUser.GetUserAsync(ct);
        if (user is null || user.Role != UserRole.Brand)
            return new(new(), 0, q.Page, q.PageSize);

        var brand = await _db.BrandProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);

        var query = _db.InfluencerProfiles
            .Where(p => p.IsActive);

        if (!string.IsNullOrEmpty(q.Q))
        {
            var needle = q.Q.ToLower();
            query = query.Where(p =>
                (p.TiktokHandle != null && p.TiktokHandle.ToLower().Contains(needle))
                || (p.Bio != null && p.Bio.ToLower().Contains(needle)));
        }

        if (!string.IsNullOrEmpty(q.Category))
            query = query.Where(p => p.Categories.Contains(q.Category));

        if (!string.IsNullOrEmpty(q.City))
            query = query.Where(p => p.City == q.City);

        if (q.MinFollowers.HasValue)
            query = query.Where(p => p.FollowerCount >= q.MinFollowers.Value);

        if (q.MaxFollowers.HasValue)
            query = query.Where(p => p.FollowerCount <= q.MaxFollowers.Value);

        if (q.MinEngagement.HasValue)
            query = query.Where(p => p.EngagementRate >= q.MinEngagement.Value);

        if (!string.IsNullOrEmpty(q.Tier))
        {
            query = q.Tier.ToLower() switch
            {
                "nano" => query.Where(p => p.FollowerCount < 10_000),
                "micro" => query.Where(p =>
                    p.FollowerCount >= 10_000 && p.FollowerCount < 100_000),
                "mid_tier" => query.Where(p => p.FollowerCount >= 100_000),
                _ => query,
            };
        }

        if (q.VerifiedOnly)
            query = query.Where(p => p.IsVerified);

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(p => p.EngagementRate)
            .ThenByDescending(p => p.FollowerCount)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);

        // Mark favorited for this brand
        Guid[] favoritedIds = Array.Empty<Guid>();
        if (brand is not null)
        {
            favoritedIds = await _db.BrandFavorites
                .Where(f => f.BrandProfileId == brand.Id
                    && rows.Select(r => r.Id).Contains(f.InfluencerProfileId))
                .Select(f => f.InfluencerProfileId)
                .ToArrayAsync(ct);
        }

        var items = rows.Select(p => new CreatorCardDto(
                p.Id,
                p.TiktokHandle,
                p.FollowerCount,
                p.EngagementRate,
                TierFor(p.FollowerCount),
                p.City,
                p.Categories.ToArray(),
                p.Bio,
                p.IsVerified,
                favoritedIds.Contains(p.Id)))
            .ToList();

        return new SearchCreatorsResult(items, total, q.Page, q.PageSize);
    }

    private static string TierFor(int f) => f switch
    {
        < 10_000 => "nano",
        < 100_000 => "micro",
        _ => "mid_tier",
    };
}
