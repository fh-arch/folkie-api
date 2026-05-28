using FluentValidation;
using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Creator.PublishSubmission;

public sealed record PublishSubmissionCommand(Guid SubmissionId, string TiktokUrl) : IRequest<Unit>;

public sealed class PublishSubmissionValidator : AbstractValidator<PublishSubmissionCommand>
{
    public PublishSubmissionValidator()
    {
        RuleFor(x => x.TiktokUrl)
            .NotEmpty()
            .Matches(@"^https?://(www\.|vm\.|vt\.)?tiktok\.com/")
            .WithMessage("Geçerli bir TikTok yayın linki gerekli.");
    }
}

public sealed class PublishSubmissionHandler : IRequestHandler<PublishSubmissionCommand, Unit>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public PublishSubmissionHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(PublishSubmissionCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Influencer) throw new ForbiddenException();

        var profile = await _db.InfluencerProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct)
            ?? throw new NotFoundException("Creator profili");

        var submission = await _db.ContentSubmissions
            .FirstOrDefaultAsync(s => s.Id == cmd.SubmissionId, ct)
            ?? throw new NotFoundException("İçerik");

        var application = await _db.CampaignApplications
            .FirstAsync(a => a.Id == submission.ApplicationId, ct);
        if (application.InfluencerProfileId != profile.Id)
            throw new ForbiddenException("Bu içerik sana ait değil.");

        submission.Publish(cmd.TiktokUrl);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
