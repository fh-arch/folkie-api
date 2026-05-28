using Folkie.Application.Common.Exceptions;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Application.Admin.GetStats;

public sealed record GetStatsQuery() : IRequest<AdminStatsDto>;

public sealed record AdminStatsDto(
    int TotalUsers,
    int TotalBrands,
    int TotalCreators,
    int NewUsersThisWeek,
    int TotalCampaigns,
    int ActiveCampaigns,
    int DraftCampaigns,
    int CompletedCampaigns,
    int TotalApplications,
    int PendingApplications,
    int ApprovedApplications,
    int TotalSubmissions,
    int SubmissionsAwaitingReview,
    int PublishedSubmissions,
    decimal TotalGmv,
    decimal PendingCreatorPayouts,
    int PendingPaymentCount);

public sealed class GetStatsHandler : IRequestHandler<GetStatsQuery, AdminStatsDto>
{
    private readonly IFolkieDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetStatsHandler(IFolkieDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AdminStatsDto> Handle(GetStatsQuery q, CancellationToken ct)
    {
        await EnsureAdmin(ct);

        var weekAgo = DateTimeOffset.UtcNow.AddDays(-7);

        var totalUsers = await _db.Users.CountAsync(ct);
        var totalBrands = await _db.Users.CountAsync(u => u.Role == UserRole.Brand, ct);
        var totalCreators = await _db.Users.CountAsync(u => u.Role == UserRole.Influencer, ct);
        var newUsersThisWeek = await _db.Users.CountAsync(u => u.CreatedAt >= weekAgo, ct);

        var totalCampaigns = await _db.Campaigns.CountAsync(ct);
        var activeCampaigns = await _db.Campaigns.CountAsync(
            c => c.Status == CampaignStatus.Active || c.Status == CampaignStatus.InProgress, ct);
        var draftCampaigns = await _db.Campaigns.CountAsync(c => c.Status == CampaignStatus.Draft, ct);
        var completedCampaigns = await _db.Campaigns.CountAsync(c => c.Status == CampaignStatus.Completed, ct);

        var totalApplications = await _db.CampaignApplications.CountAsync(ct);
        var pendingApplications = await _db.CampaignApplications.CountAsync(
            a => a.Status == ApplicationStatus.Pending, ct);
        var approvedApplications = await _db.CampaignApplications.CountAsync(
            a => a.Status == ApplicationStatus.Approved, ct);

        var totalSubmissions = await _db.ContentSubmissions.CountAsync(ct);
        var awaitingReview = await _db.ContentSubmissions.CountAsync(
            s => s.Status == SubmissionStatus.Submitted, ct);
        var publishedSubmissions = await _db.ContentSubmissions.CountAsync(
            s => s.Status == SubmissionStatus.Published, ct);

        // GMV = total approved-application amounts (committed creator earnings + brand spending)
        var gmv = await (
            from a in _db.CampaignApplications
            where a.Status == ApplicationStatus.Approved
            join c in _db.Campaigns on a.CampaignId equals c.Id
            select c.BudgetPerInfluencer
        ).SumAsync(ct);

        // Pending payouts to creators: payments not yet transferred
        var pendingPaymentCount = await _db.Payments.CountAsync(
            p => p.Status != PaymentStatus.Transferred && p.Status != PaymentStatus.Failed, ct);
        var pendingPayoutSum = await _db.Payments
            .Where(p => p.Status != PaymentStatus.Transferred && p.Status != PaymentStatus.Failed)
            .SumAsync(p => p.Amount, ct);

        return new AdminStatsDto(
            totalUsers,
            totalBrands,
            totalCreators,
            newUsersThisWeek,
            totalCampaigns,
            activeCampaigns,
            draftCampaigns,
            completedCampaigns,
            totalApplications,
            pendingApplications,
            approvedApplications,
            totalSubmissions,
            awaitingReview,
            publishedSubmissions,
            gmv,
            pendingPayoutSum,
            pendingPaymentCount);
    }

    private async Task EnsureAdmin(CancellationToken ct)
    {
        var user = await _currentUser.RequireUserAsync(ct);
        if (user.Role != UserRole.Admin)
            throw new ForbiddenException("Admin yetkisi gerekli.");
    }
}
