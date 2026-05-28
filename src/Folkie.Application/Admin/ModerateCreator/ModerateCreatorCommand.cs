using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Admin.ModerateCreator;

public enum CreatorModerationAction
{
    Verify,
    Suspend,
    Reactivate,
}

public sealed record ModerateCreatorCommand(
    Guid InfluencerProfileId,
    CreatorModerationAction Action) : IRequest<Unit>;

public sealed class ModerateCreatorHandler : IRequestHandler<ModerateCreatorCommand, Unit>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ModerateCreatorHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(ModerateCreatorCommand c, CancellationToken ct)
    {
        await EnsureAdmin(ct);

        var profile = await _db.InfluencerProfiles
            .FirstOrDefaultAsync(p => p.Id == c.InfluencerProfileId, ct)
            ?? throw new NotFoundException("Creator profili bulunamadı");

        switch (c.Action)
        {
            case CreatorModerationAction.Verify:
                profile.Verify();
                break;
            case CreatorModerationAction.Suspend:
                profile.Deactivate();
                break;
            case CreatorModerationAction.Reactivate:
                profile.Activate();
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
