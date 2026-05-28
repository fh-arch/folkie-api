using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using Folkie.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Brand.SubmitCampaign;

/// <summary>
/// Draft kampanyayı onaya gönderir + brand_payment kaydı oluşturur.
/// Marka bu noktada ödeme bilgilerini görür; admin dekontu onaylayınca kampanya active olur.
/// </summary>
public sealed record SubmitCampaignCommand(Guid CampaignId) : IRequest<SubmitCampaignResult>;

public sealed record SubmitCampaignResult(
    Guid CampaignId,
    Guid BrandPaymentId,
    decimal AmountDue,
    string Status);

public sealed class SubmitCampaignHandler : IRequestHandler<SubmitCampaignCommand, SubmitCampaignResult>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public SubmitCampaignHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SubmitCampaignResult> Handle(SubmitCampaignCommand cmd, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Brand)
            throw new ForbiddenException();

        var brand = await _db.BrandProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct)
            ?? throw new NotFoundException("Marka profili");

        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == cmd.CampaignId && c.BrandProfileId == brand.Id, ct)
            ?? throw new NotFoundException("Kampanya");

        campaign.SubmitForPayment();

        var totalWithFee = campaign.TotalBudget * (1 + campaign.PlatformFeeRate / 100m);
        var payment = BrandPayment.Create(campaign.Id, brand.Id, totalWithFee);
        _db.BrandPayments.Add(payment);

        await _db.SaveChangesAsync(ct);

        return new SubmitCampaignResult(
            campaign.Id,
            payment.Id,
            totalWithFee,
            campaign.Status.ToString().ToLower());
    }
}
