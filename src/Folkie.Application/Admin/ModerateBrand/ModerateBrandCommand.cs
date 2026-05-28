using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Admin.ModerateBrand;

public enum BrandModerationAction
{
    Verify,
    Suspend,
    Reactivate,
}

public sealed record ModerateBrandCommand(Guid BrandProfileId, BrandModerationAction Action)
    : IRequest<Unit>;

public sealed class ModerateBrandHandler : IRequestHandler<ModerateBrandCommand, Unit>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ModerateBrandHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(ModerateBrandCommand c, CancellationToken ct)
    {
        await EnsureAdmin(ct);

        var brand = await _db.BrandProfiles
            .FirstOrDefaultAsync(b => b.Id == c.BrandProfileId, ct)
            ?? throw new NotFoundException("Marka profili bulunamadı");

        switch (c.Action)
        {
            case BrandModerationAction.Verify:
                brand.Verify();
                break;
            case BrandModerationAction.Suspend:
                brand.Deactivate();
                break;
            case BrandModerationAction.Reactivate:
                brand.Activate();
                break;
        }

        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private async Task EnsureAdmin(CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Admin)
            throw new ForbiddenException("Admin yetkisi gerekli.");
    }
}
