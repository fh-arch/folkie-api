using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Brand.GetCreator;

public sealed record GetCreatorForBrandQuery(Guid ProfileId) : IRequest<CreatorDetailDto>;

public sealed record CreatorDetailDto(
    Guid Id,
    string? Handle,
    string? AvatarUrl,
    string? Bio,
    int FollowerCount,
    decimal EngagementRate,
    string Tier,
    string? City,
    string[] Categories,
    string[] ContentLanguage,
    int? LikesCount,
    int? VideoCount,
    bool IsVerified,
    bool IsFavorited,
    Guid UserId);

public sealed class GetCreatorForBrandHandler
    : IRequestHandler<GetCreatorForBrandQuery, CreatorDetailDto>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCreatorForBrandHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CreatorDetailDto> Handle(GetCreatorForBrandQuery q, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Brand) throw new ForbiddenException();

        var profile = await _db.InfluencerProfiles
            .FirstOrDefaultAsync(p => p.Id == q.ProfileId && p.IsActive, ct)
            ?? throw new NotFoundException("Creator");

        var brand = await _db.BrandProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);

        bool isFavorited = false;
        if (brand is not null)
        {
            isFavorited = await _db.BrandFavorites.AnyAsync(f =>
                f.BrandProfileId == brand.Id &&
                f.InfluencerProfileId == profile.Id, ct);
        }

        var tier = profile.FollowerCount switch
        {
            < 10_000 => "nano",
            < 100_000 => "micro",
            _ => "mid_tier",
        };

        return new CreatorDetailDto(
            profile.Id,
            profile.TiktokHandle,
            profile.TiktokAvatarUrl,
            profile.Bio,
            profile.FollowerCount,
            profile.EngagementRate,
            tier,
            profile.City,
            profile.Categories.ToArray(),
            profile.ContentLanguage.ToArray(),
            profile.TiktokLikesCount,
            profile.TiktokVideoCount,
            profile.IsVerified,
            isFavorited,
            profile.UserId);
    }
}
