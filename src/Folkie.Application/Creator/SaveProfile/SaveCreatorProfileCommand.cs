using FluentValidation;
using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Application.Creator.GetMyProfile;
using Folkie.Domain.Common;
using Folkie.Domain.Influencers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Creator.SaveProfile;

/// <summary>
/// Upsert: profil yoksa oluşturur, varsa günceller.
/// Onboarding ve "Profilim" sayfası bu komutu kullanır.
/// </summary>
public sealed record SaveCreatorProfileCommand(
    string? Bio,
    string? City,
    string[] Categories,
    string[] Subcategories,
    string[] ContentLanguage,
    string? Iban,           // null/empty bırakılırsa değişmez
    string? IbanName) : IRequest<CreatorProfileDto>;

public sealed class SaveCreatorProfileValidator : AbstractValidator<SaveCreatorProfileCommand>
{
    public SaveCreatorProfileValidator()
    {
        RuleFor(x => x.Bio).MaximumLength(1000);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Categories)
            .NotEmpty().WithMessage("En az bir kategori seç.")
            .Must(c => c.Length <= 5).WithMessage("En fazla 5 kategori seçebilirsin.");
        RuleFor(x => x.ContentLanguage)
            .NotEmpty().WithMessage("En az bir içerik dili seç.");
        When(x => !string.IsNullOrEmpty(x.Iban), () =>
        {
            RuleFor(x => x.Iban!)
                .Matches(@"^TR\d{24}$").WithMessage("Geçersiz IBAN. TR ile başlayan 26 hane olmalı.");
            RuleFor(x => x.IbanName)
                .NotEmpty().WithMessage("IBAN sahibinin adı zorunlu.")
                .MaximumLength(255);
        });
    }
}

public sealed class SaveCreatorProfileHandler
    : IRequestHandler<SaveCreatorProfileCommand, CreatorProfileDto>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IIbanProtector _ibanProtector;

    public SaveCreatorProfileHandler(
        IFolkieDbContext db,
        ICurrentUser currentUser,
        IIbanProtector ibanProtector)
    {
        _db = db;
        _currentUser = currentUser;
        _ibanProtector = ibanProtector;
    }

    public async Task<CreatorProfileDto> Handle(
        SaveCreatorProfileCommand cmd,
        CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Influencer)
            throw new ForbiddenException("Sadece creator rolündeki kullanıcılar bu profili oluşturabilir.");

        var profile = await _db.InfluencerProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);

        if (profile is null)
        {
            profile = InfluencerProfile.Create(user.Id, cmd.City, cmd.Categories);
            _db.InfluencerProfiles.Add(profile);
        }

        profile.UpdateBio(cmd.Bio, cmd.City, cmd.Categories, cmd.Subcategories);

        if (!string.IsNullOrEmpty(cmd.Iban) && !string.IsNullOrEmpty(cmd.IbanName))
        {
            profile.SetIban(_ibanProtector.Protect(cmd.Iban), cmd.IbanName);
        }

        await _db.SaveChangesAsync(ct);

        return new CreatorProfileDto(
            profile.Id,
            profile.UserId,
            profile.TiktokHandle,
            profile.FollowerCount,
            profile.EngagementRate,
            profile.Tier.ToString().ToLower(),
            profile.City,
            profile.Country,
            profile.ContentLanguage.ToArray(),
            profile.Categories.ToArray(),
            profile.Subcategories.ToArray(),
            profile.Bio,
            profile.IbanName,
            profile.Iban is not null && !profile.Iban.IsEmpty,
            profile.IsVerified,
            profile.IsActive,
            profile.LastTiktokSync,
            profile.CreatedAt);
    }
}
