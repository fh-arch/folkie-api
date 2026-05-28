using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Creator.Badge;

public sealed record GetMyBadgeQuery() : IRequest<BadgeDto>;

public sealed record BadgeDto(
    string Level,
    string LevelLabel,
    string Emoji,
    int CompletedCampaigns,
    decimal AvgRating,
    int NextLevelGap,
    string NextLevel,
    decimal ProgressPercent);

public sealed class GetMyBadgeHandler : IRequestHandler<GetMyBadgeQuery, BadgeDto>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyBadgeHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BadgeDto> Handle(GetMyBadgeQuery _, CancellationToken ct)
    {
        var user = await _currentUser.GetUserAsync(ct);
        if (user is null || user.Role != UserRole.Influencer)
            return EmptyBadge();

        var profile = await _db.InfluencerProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (profile is null) return EmptyBadge();

        var completedCount = await (
            from a in _db.CampaignApplications
            join s in _db.ContentSubmissions on a.Id equals s.ApplicationId
            where a.InfluencerProfileId == profile.Id
                && s.Status == SubmissionStatus.Published
            select a.Id
        ).CountAsync(ct);

        var avgRating = await _db.Reviews
            .Where(r => r.RevieweeId == user.Id
                && r.ReviewerRole == ReviewerRole.Brand)
            .Select(r => (decimal?)r.Score)
            .AverageAsync(ct) ?? 0m;

        return Compute(completedCount, avgRating);
    }

    private static BadgeDto Compute(int count, decimal avg)
    {
        if (count >= 15 && avg >= 4.5m)
            return new("super_star", "Süper Star", "🌟⭐", count, avg, 0, "—", 100);

        if (count >= 5 && avg >= 4.0m)
        {
            var gap = Math.Max(0, 15 - count);
            return new("shining", "Parlayan", "⭐", count, avg, gap, "Süper Star",
                Math.Round((decimal)(count - 5) * 100m / 10m, 0));
        }

        if (count >= 1)
        {
            var gap = Math.Max(0, 5 - count);
            return new("rising", "Yükselen", "🌟", count, avg, gap, "Parlayan",
                Math.Round((decimal)count * 100m / 5m, 0));
        }

        return new("welcome", "Hoşgeldin", "👋", count, avg, 1, "Yükselen", 0);
    }

    private static BadgeDto EmptyBadge() =>
        new("welcome", "Hoşgeldin", "👋", 0, 0m, 1, "Yükselen", 0);
}
