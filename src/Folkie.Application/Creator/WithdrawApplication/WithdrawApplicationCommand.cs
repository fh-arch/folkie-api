using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Creator.WithdrawApplication;

public sealed record WithdrawApplicationCommand(Guid ApplicationId) : IRequest<Unit>;

public sealed class WithdrawApplicationHandler : IRequestHandler<WithdrawApplicationCommand, Unit>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public WithdrawApplicationHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(WithdrawApplicationCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Influencer)
            throw new ForbiddenException();

        var profile = await _db.InfluencerProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct)
            ?? throw new NotFoundException("Creator profili");

        var application = await _db.CampaignApplications
            .FirstOrDefaultAsync(a => a.Id == cmd.ApplicationId && a.InfluencerProfileId == profile.Id, ct)
            ?? throw new NotFoundException("Başvuru");

        application.Withdraw();
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
