using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Creator.GetMyProfile;

public sealed record GetMyCreatorProfileQuery() : IRequest<CreatorProfileDto?>;

public sealed record CreatorProfileDto(
    Guid Id,
    Guid UserId,
    string? TiktokHandle,
    int FollowerCount,
    decimal EngagementRate,
    string Tier,
    string? City,
    string Country,
    string[] ContentLanguage,
    string[] Categories,
    string[] Subcategories,
    string? Bio,
    string? IbanName,
    bool HasIban,
    bool IsVerified,
    bool IsActive,
    DateTimeOffset? LastTiktokSync,
    DateTimeOffset CreatedAt);

public sealed class GetMyCreatorProfileHandler
    : IRequestHandler<GetMyCreatorProfileQuery, CreatorProfileDto?>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyCreatorProfileHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CreatorProfileDto?> Handle(
        GetMyCreatorProfileQuery query,
        CancellationToken ct)
    {
        var user = await _currentUser.GetUserAsync(ct);
        if (user is null || user.Role != UserRole.Influencer)
            return null;

        return await _db.InfluencerProfiles
            .Where(p => p.UserId == user.Id)
            .Select(p => new CreatorProfileDto(
                p.Id,
                p.UserId,
                p.TiktokHandle,
                p.FollowerCount,
                p.EngagementRate,
                p.Tier.ToString().ToLower(),
                p.City,
                p.Country,
                p.ContentLanguage.ToArray(),
                p.Categories.ToArray(),
                p.Subcategories.ToArray(),
                p.Bio,
                p.IbanName,
                p.Iban != null && !p.Iban.IsEmpty,
                p.IsVerified,
                p.IsActive,
                p.LastTiktokSync,
                p.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
