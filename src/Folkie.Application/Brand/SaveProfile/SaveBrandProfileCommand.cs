using FluentValidation;
using Folkie.Application.Brand.GetMyProfile;
using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Brands;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Brand.SaveProfile;

public sealed record SaveBrandProfileCommand(
    string BrandName,
    string? TaxId,
    string? Industry,
    string? Website,
    string? LogoUrl,
    string? ContactName,
    string? ContactPhone,
    string? BillingAddress) : IRequest<BrandProfileDto>;

public sealed class SaveBrandProfileValidator : AbstractValidator<SaveBrandProfileCommand>
{
    public SaveBrandProfileValidator()
    {
        RuleFor(x => x.BrandName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.TaxId).MaximumLength(50);
        RuleFor(x => x.Industry).MaximumLength(100);
        RuleFor(x => x.Website).MaximumLength(2048);
        RuleFor(x => x.LogoUrl).MaximumLength(2048);
        RuleFor(x => x.ContactName).MaximumLength(255);
        RuleFor(x => x.ContactPhone).MaximumLength(50);
        RuleFor(x => x.BillingAddress).MaximumLength(500);
    }
}

public sealed class SaveBrandProfileHandler
    : IRequestHandler<SaveBrandProfileCommand, BrandProfileDto>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public SaveBrandProfileHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BrandProfileDto> Handle(SaveBrandProfileCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Brand)
            throw new ForbiddenException("Sadece marka rolündeki kullanıcılar bu profili oluşturabilir.");

        var profile = await _db.BrandProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);

        if (profile is null)
        {
            profile = BrandProfile.Create(user.Id, cmd.BrandName);
            _db.BrandProfiles.Add(profile);
        }

        profile.UpdateProfile(
            cmd.BrandName,
            cmd.TaxId,
            cmd.Industry,
            cmd.Website,
            cmd.LogoUrl,
            cmd.ContactName,
            cmd.ContactPhone,
            cmd.BillingAddress);

        await _db.SaveChangesAsync(ct);

        return new BrandProfileDto(
            profile.Id,
            profile.UserId,
            profile.BrandName,
            profile.TaxId,
            profile.Industry,
            profile.Website,
            profile.LogoUrl,
            profile.ContactName,
            profile.ContactPhone,
            profile.BillingAddress,
            profile.IsVerified,
            profile.IsActive,
            profile.CreatedAt);
    }
}
