using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Admin.ListPayments;

public sealed record ListPaymentsQuery(string? Status) : IRequest<List<AdminPaymentDto>>;

public sealed record AdminPaymentDto(
    Guid Id,
    Guid CampaignId,
    string CampaignTitle,
    Guid InfluencerProfileId,
    string? CreatorHandle,
    decimal Amount,
    string PaymentType,
    string Status,
    string IbanMasked,
    string IbanName,
    string? AdminNote,
    string? TransferReference,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? TransferredAt,
    DateTimeOffset CreatedAt);

public sealed class ListPaymentsHandler : IRequestHandler<ListPaymentsQuery, List<AdminPaymentDto>>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IIbanProtector _ibanProtector;

    public ListPaymentsHandler(
        IFolkieDbContext db,
        ICurrentUser currentUser,
        IIbanProtector ibanProtector)
    {
        _db = db;
        _currentUser = currentUser;
        _ibanProtector = ibanProtector;
    }

    public async Task<List<AdminPaymentDto>> Handle(ListPaymentsQuery q, CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Admin)
            throw new ForbiddenException("Admin yetkisi gerekli.");

        var query = _db.Payments.AsQueryable();
        if (!string.IsNullOrEmpty(q.Status)
            && Enum.TryParse<PaymentStatus>(q.Status, ignoreCase: true, out var s))
        {
            query = query.Where(p => p.Status == s);
        }

        var rows = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.CampaignId,
                CampaignTitle = _db.Campaigns.Where(c => c.Id == p.CampaignId).Select(c => c.Title).FirstOrDefault() ?? "",
                p.InfluencerProfileId,
                CreatorHandle = _db.InfluencerProfiles
                    .Where(ip => ip.Id == p.InfluencerProfileId)
                    .Select(ip => ip.TiktokHandle)
                    .FirstOrDefault(),
                p.Amount,
                p.PaymentType,
                p.Status,
                p.Iban,
                p.IbanName,
                p.AdminNote,
                p.TransferReference,
                p.ApprovedAt,
                p.TransferredAt,
                p.CreatedAt,
            })
            .ToListAsync(ct);

        return rows.Select(r =>
        {
            string masked;
            try
            {
                var plain = _ibanProtector.Unprotect(r.Iban);
                masked = plain.Length > 8
                    ? $"{plain[..6]}…{plain[^4..]}"
                    : "TR***";
            }
            catch
            {
                masked = "(şifreleme hatası)";
            }

            return new AdminPaymentDto(
                r.Id,
                r.CampaignId,
                r.CampaignTitle,
                r.InfluencerProfileId,
                r.CreatorHandle,
                r.Amount,
                r.PaymentType.ToString().ToLower(),
                r.Status.ToString().ToLower(),
                masked,
                r.IbanName,
                r.AdminNote,
                r.TransferReference,
                r.ApprovedAt,
                r.TransferredAt,
                r.CreatedAt);
        }).ToList();
    }
}
