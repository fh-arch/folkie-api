using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Brand.GetMyProfile;

public sealed record GetMyBrandProfileQuery() : IRequest<BrandProfileDto?>;

public sealed record BrandProfileDto(
    Guid Id,
    Guid UserId,
    string BrandName,
    string? TaxId,
    string? Industry,
    string? Website,
    string? LogoUrl,
    string? ContactName,
    string? ContactPhone,
    string? BillingAddress,
    bool IsVerified,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed class GetMyBrandProfileHandler
    : IRequestHandler<GetMyBrandProfileQuery, BrandProfileDto?>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyBrandProfileHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BrandProfileDto?> Handle(GetMyBrandProfileQuery _, CancellationToken ct)
    {
        var user = await _currentUser.GetUserAsync(ct);
        if (user is null || user.Role != UserRole.Brand)
            return null;

        return await _db.BrandProfiles
            .Where(p => p.UserId == user.Id)
            .Select(p => new BrandProfileDto(
                p.Id,
                p.UserId,
                p.BrandName,
                p.TaxId,
                p.Industry,
                p.Website,
                p.LogoUrl,
                p.ContactName,
                p.ContactPhone,
                p.BillingAddress,
                p.IsVerified,
                p.IsActive,
                p.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
