using FluentValidation;
using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Campaigns;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Brand.CreateCampaign;

public sealed record CreateCampaignCommand(
    string Title,
    string ProductName,
    string ProductCategory,
    string Brief,
    string[]? RequiredHashtags,
    string[]? ContentTypes,           // ["video","stitch",...]
    string? Tone,
    string[]? TargetCategories,
    string[]? TargetCities,
    string[]? ContentLanguage,
    int MinFollowers,
    int MaxFollowers,
    int InfluencerCount,
    decimal BudgetPerInfluencer,
    string? ProductDelivery,          // "physical" | "digital" | "none"
    DateOnly ApplicationDeadline,
    DateOnly PublishStartDate,
    DateOnly PublishEndDate,
    bool IsFlashCampaign,
    TimeOnly? FlashPublishTime) : IRequest<Guid>;

public sealed class CreateCampaignValidator : AbstractValidator<CreateCampaignCommand>
{
    public CreateCampaignValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ProductCategory).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Brief).NotEmpty().MinimumLength(20);
        RuleFor(x => x.MinFollowers).GreaterThanOrEqualTo(1000);
        RuleFor(x => x.MaxFollowers).GreaterThan(x => x.MinFollowers);
        RuleFor(x => x.InfluencerCount).InclusiveBetween(1, 10000);
        RuleFor(x => x.BudgetPerInfluencer).GreaterThanOrEqualTo(500m);
        RuleFor(x => x.PublishStartDate).GreaterThan(x => x.ApplicationDeadline)
            .WithMessage("Yayın başlangıcı, son başvuru tarihinden sonra olmalı.");
        RuleFor(x => x.PublishEndDate).GreaterThanOrEqualTo(x => x.PublishStartDate);
        When(x => x.IsFlashCampaign, () =>
        {
            RuleFor(x => x.FlashPublishTime)
                .NotNull().WithMessage("Flash kampanyada yayın saati zorunlu.");
        });
    }
}

public sealed class CreateCampaignHandler : IRequestHandler<CreateCampaignCommand, Guid>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateCampaignHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateCampaignCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Brand)
            throw new ForbiddenException("Sadece marka rolündeki kullanıcılar kampanya oluşturabilir.");

        var brand = await _db.BrandProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct)
            ?? throw new NotFoundException("Marka profili");

        var campaign = Campaign.CreateDraft(
            brand.Id,
            cmd.Title,
            cmd.ProductName,
            cmd.ProductCategory,
            cmd.Brief,
            cmd.InfluencerCount,
            cmd.BudgetPerInfluencer,
            cmd.ApplicationDeadline,
            cmd.PublishStartDate,
            cmd.PublishEndDate);

        var contentTypes = (cmd.ContentTypes ?? new[] { "video" })
            .Select(c => Enum.Parse<ContentType>(c, ignoreCase: true))
            .ToList();

        campaign.UpdateBasics(
            cmd.Title,
            cmd.ProductName,
            cmd.ProductCategory,
            cmd.Brief,
            cmd.RequiredHashtags ?? Array.Empty<string>(),
            contentTypes,
            cmd.Tone);

        campaign.UpdateTargeting(
            cmd.TargetCategories ?? Array.Empty<string>(),
            cmd.TargetCities ?? Array.Empty<string>(),
            cmd.ContentLanguage ?? new[] { "tr" },
            cmd.MinFollowers,
            cmd.MaxFollowers);

        campaign.UpdateBudget(cmd.InfluencerCount, cmd.BudgetPerInfluencer);

        if (cmd.IsFlashCampaign && cmd.FlashPublishTime.HasValue)
            campaign.SetFlashCampaign(cmd.FlashPublishTime.Value);

        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync(ct);

        return campaign.Id;
    }
}
