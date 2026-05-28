using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Brands;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Brand.Favorites;

/* ─── List ─────────────────────────────────────────────────── */

public sealed record ListFavoritesQuery() : IRequest<List<FavoriteCreatorDto>>;

public sealed record FavoriteCreatorDto(
    Guid Id,                          // BrandFavorite id
    Guid InfluencerProfileId,
    string? Handle,
    int FollowerCount,
    decimal EngagementRate,
    string Tier,
    string? City,
    string[] Categories,
    bool IsVerified,
    string? Note,
    DateTimeOffset AddedAt);

public sealed class ListFavoritesHandler
    : IRequestHandler<ListFavoritesQuery, List<FavoriteCreatorDto>>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListFavoritesHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<FavoriteCreatorDto>> Handle(
        ListFavoritesQuery _,
        CancellationToken ct)
    {
        var user = await _currentUser.GetUserAsync(ct);
        if (user is null || user.Role != UserRole.Brand) return new();

        var brand = await _db.BrandProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (brand is null) return new();

        var rows = await (
            from f in _db.BrandFavorites
            join p in _db.InfluencerProfiles on f.InfluencerProfileId equals p.Id
            where f.BrandProfileId == brand.Id
            orderby f.CreatedAt descending
            select new
            {
                FavoriteId = f.Id,
                CreatorId = p.Id,
                p.TiktokHandle,
                p.FollowerCount,
                p.EngagementRate,
                p.City,
                p.Categories,
                p.IsVerified,
                f.Note,
                AddedAt = f.CreatedAt,
            }).ToListAsync(ct);

        return rows.Select(r => new FavoriteCreatorDto(
                r.FavoriteId,
                r.CreatorId,
                r.TiktokHandle,
                r.FollowerCount,
                r.EngagementRate,
                TierFor(r.FollowerCount),
                r.City,
                r.Categories.ToArray(),
                r.IsVerified,
                r.Note,
                r.AddedAt))
            .ToList();
    }

    private static string TierFor(int f) => f switch
    {
        < 10_000 => "nano",
        < 100_000 => "micro",
        _ => "mid_tier",
    };
}

/* ─── Add ──────────────────────────────────────────────────── */

public sealed record AddFavoriteCommand(Guid InfluencerProfileId, string? Note)
    : IRequest<Guid>;

public sealed class AddFavoriteHandler : IRequestHandler<AddFavoriteCommand, Guid>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AddFavoriteHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(AddFavoriteCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Brand) throw new ForbiddenException();

        var brand = await _db.BrandProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct)
            ?? throw new NotFoundException("Marka profili");

        var creator = await _db.InfluencerProfiles
            .FirstOrDefaultAsync(p => p.Id == cmd.InfluencerProfileId, ct)
            ?? throw new NotFoundException("Creator");

        var existing = await _db.BrandFavorites
            .FirstOrDefaultAsync(f =>
                f.BrandProfileId == brand.Id
                && f.InfluencerProfileId == creator.Id, ct);
        if (existing is not null)
        {
            existing.UpdateNote(cmd.Note);
            await _db.SaveChangesAsync(ct);
            return existing.Id;
        }

        var favorite = BrandFavorite.Create(brand.Id, creator.Id, cmd.Note);
        _db.BrandFavorites.Add(favorite);
        await _db.SaveChangesAsync(ct);
        return favorite.Id;
    }
}

/* ─── Remove ───────────────────────────────────────────────── */

public sealed record RemoveFavoriteCommand(Guid InfluencerProfileId) : IRequest<Unit>;

public sealed class RemoveFavoriteHandler : IRequestHandler<RemoveFavoriteCommand, Unit>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public RemoveFavoriteHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(RemoveFavoriteCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Brand) throw new ForbiddenException();

        var brand = await _db.BrandProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct)
            ?? throw new NotFoundException("Marka profili");

        var fav = await _db.BrandFavorites
            .FirstOrDefaultAsync(f =>
                f.BrandProfileId == brand.Id
                && f.InfluencerProfileId == cmd.InfluencerProfileId, ct);
        if (fav is null) return Unit.Value;

        _db.BrandFavorites.Remove(fav);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
