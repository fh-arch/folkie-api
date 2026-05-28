using FluentValidation;
using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using Folkie.Domain.Submissions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Creator.CreateSubmission;

public sealed record CreateSubmissionCommand(
    Guid ApplicationId,
    string? VideoUrl,
    string? Script,
    string[] Hashtags) : IRequest<Guid>;

public sealed class CreateSubmissionValidator : AbstractValidator<CreateSubmissionCommand>
{
    public CreateSubmissionValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Script).MaximumLength(5000);
        RuleFor(x => x).Must(c => !string.IsNullOrEmpty(c.VideoUrl) || !string.IsNullOrEmpty(c.Script))
            .WithMessage("Video URL veya script'ten en az biri gerekli.");
    }
}

public sealed class CreateSubmissionHandler : IRequestHandler<CreateSubmissionCommand, Guid>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;

    public CreateSubmissionHandler(
        IFolkieDbContext db,
        ICurrentUser currentUser,
        INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<Guid> Handle(CreateSubmissionCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Influencer)
            throw new ForbiddenException();

        var profile = await _db.InfluencerProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct)
            ?? throw new NotFoundException("Creator profili");

        var application = await _db.CampaignApplications
            .FirstOrDefaultAsync(a => a.Id == cmd.ApplicationId, ct)
            ?? throw new NotFoundException("Başvuru");

        if (application.InfluencerProfileId != profile.Id)
            throw new ForbiddenException("Bu başvuru sana ait değil.");

        if (application.Status != ApplicationStatus.Approved)
            throw new ConflictException("İçerik yüklemek için başvurunun onaylanmış olması gerekir.");

        var submission = ContentSubmission.Create(
            application.Id,
            cmd.VideoUrl,
            cmd.Script,
            cmd.Hashtags);

        _db.ContentSubmissions.Add(submission);
        await _db.SaveChangesAsync(ct);

        // Notify brand
        var campaign = await _db.Campaigns.FirstAsync(c => c.Id == application.CampaignId, ct);
        var brand = await _db.BrandProfiles.FirstAsync(b => b.Id == campaign.BrandProfileId, ct);
        await _notifications.NotifyAsync(
            brand.UserId,
            "submission.created",
            "Yeni içerik teslimi",
            $"{profile.TiktokHandle ?? "Bir creator"} \"{campaign.Title}\" için içerik yükledi.",
            new { campaignId = campaign.Id, submissionId = submission.Id },
            ct);

        return submission.Id;
    }
}
