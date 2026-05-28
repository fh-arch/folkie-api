using FluentValidation;
using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Brand.ReviewSubmission;

public sealed record ApproveSubmissionCommand(Guid SubmissionId) : IRequest<Unit>;

public sealed class ApproveSubmissionHandler : IRequestHandler<ApproveSubmissionCommand, Unit>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ApproveSubmissionHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(ApproveSubmissionCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Brand) throw new ForbiddenException();

        var brand = await _db.BrandProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct)
            ?? throw new NotFoundException("Marka profili");

        var submission = await _db.ContentSubmissions
            .FirstOrDefaultAsync(s => s.Id == cmd.SubmissionId, ct)
            ?? throw new NotFoundException("İçerik");

        var application = await _db.CampaignApplications
            .FirstAsync(a => a.Id == submission.ApplicationId, ct);
        var campaign = await _db.Campaigns
            .FirstAsync(c => c.Id == application.CampaignId, ct);

        if (campaign.BrandProfileId != brand.Id)
            throw new ForbiddenException("Bu içeriği onaylama yetkin yok.");

        submission.Approve();
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public sealed record RequestRevisionCommand(Guid SubmissionId, string Note) : IRequest<Unit>;

public sealed class RequestRevisionValidator : AbstractValidator<RequestRevisionCommand>
{
    public RequestRevisionValidator()
    {
        RuleFor(x => x.Note).NotEmpty().MinimumLength(10).MaximumLength(1000);
    }
}

public sealed class RequestRevisionHandler : IRequestHandler<RequestRevisionCommand, Unit>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public RequestRevisionHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(RequestRevisionCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Brand) throw new ForbiddenException();

        var brand = await _db.BrandProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct)
            ?? throw new NotFoundException("Marka profili");

        var submission = await _db.ContentSubmissions
            .FirstOrDefaultAsync(s => s.Id == cmd.SubmissionId, ct)
            ?? throw new NotFoundException("İçerik");

        var application = await _db.CampaignApplications
            .FirstAsync(a => a.Id == submission.ApplicationId, ct);
        var campaign = await _db.Campaigns
            .FirstAsync(c => c.Id == application.CampaignId, ct);

        if (campaign.BrandProfileId != brand.Id)
            throw new ForbiddenException();

        submission.RequestRevision(cmd.Note);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
