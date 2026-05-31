using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Api.Endpoints;

public static class SuperAdminEndpoints
{
    public static IEndpointRouteBuilder MapSuperAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/superadmin")
            .WithTags("SuperAdmin")
            .RequireAuthorization();

        // ── Stats ────────────────────────────────────────────────
        g.MapGet("/stats", async (IFolkieDbContext db, ICurrentUser cu, CancellationToken ct) =>
        {
            if (!cu.IsSuperAdmin) return Results.Forbid();

            var totalUsers     = await db.Users.CountAsync(ct);
            var totalBrands    = await db.Users.CountAsync(u => u.Role == UserRole.Brand, ct);
            var totalCreators  = await db.Users.CountAsync(u => u.Role == UserRole.Influencer, ct);
            var blockedUsers   = await db.Users.CountAsync(u => u.IsBlocked, ct);

            var newToday       = await db.Users.CountAsync(u => u.CreatedAt >= DateTimeOffset.UtcNow.AddDays(-1), ct);
            var newThisWeek    = await db.Users.CountAsync(u => u.CreatedAt >= DateTimeOffset.UtcNow.AddDays(-7), ct);
            var newThisMonth   = await db.Users.CountAsync(u => u.CreatedAt >= DateTimeOffset.UtcNow.AddDays(-30), ct);

            var totalCampaigns = await db.Campaigns.CountAsync(ct);
            var activeCampaigns = await db.Campaigns.CountAsync(c =>
                c.Status == CampaignStatus.Active || c.Status == CampaignStatus.InProgress, ct);
            var completedCampaigns = await db.Campaigns.CountAsync(c => c.Status == CampaignStatus.Completed, ct);

            var pendingApplications = await db.CampaignApplications.CountAsync(a => a.Status == ApplicationStatus.Pending, ct);
            var pendingSubmissions  = await db.ContentSubmissions.CountAsync(s => s.Status == SubmissionStatus.Submitted, ct);
            var pendingBrandPayments = await db.BrandPayments.CountAsync(p => p.Status == BrandPaymentStatus.Pending, ct);
            var pendingCreatorPayouts = await db.Payments.CountAsync(p => p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Approved, ct);

            // Commission = sum of platform_fee_rate% on completed/active campaigns
            var campaigns = await db.Campaigns
                .Where(c => c.Status == CampaignStatus.Completed || c.Status == CampaignStatus.InProgress || c.Status == CampaignStatus.Active)
                .Select(c => new { c.TotalBudget, c.PlatformFeeRate, c.Status })
                .ToListAsync(ct);

            var totalCommission    = campaigns.Sum(c => c.TotalBudget * c.PlatformFeeRate / 100m);
            var earnedCommission   = campaigns.Where(c => c.Status == CampaignStatus.Completed).Sum(c => c.TotalBudget * c.PlatformFeeRate / 100m);
            var pendingCommission  = campaigns.Where(c => c.Status != CampaignStatus.Completed).Sum(c => c.TotalBudget * c.PlatformFeeRate / 100m);

            var totalBrandIn    = await db.BrandPayments.Where(p => p.Status == BrandPaymentStatus.Received || p.Status == BrandPaymentStatus.Partial).SumAsync(p => p.Amount, ct);
            var totalCreatorOut = await db.Payments.Where(p => p.Status == PaymentStatus.Transferred).SumAsync(p => p.Amount, ct);
            var folkieBalance   = totalBrandIn - totalCreatorOut;

            return Results.Ok(new
            {
                users = new { totalUsers, totalBrands, totalCreators, blockedUsers, newToday, newThisWeek, newThisMonth },
                campaigns = new { totalCampaigns, activeCampaigns, completedCampaigns },
                queues = new { pendingApplications, pendingSubmissions, pendingBrandPayments, pendingCreatorPayouts },
                finance = new { totalCommission, earnedCommission, pendingCommission, totalBrandIn, totalCreatorOut, folkieBalance },
            });
        }).WithName("SuperAdminStats");

        // ── Users ────────────────────────────────────────────────
        g.MapGet("/users", async (string? role, string? q, int? page, IFolkieDbContext db, ICurrentUser cu, CancellationToken ct) =>
        {
            if (!cu.IsSuperAdmin) return Results.Forbid();

            var query = db.Users.AsQueryable();
            if (!string.IsNullOrEmpty(q))
                query = query.Where(u => u.Email.Contains(q) || (u.FullName != null && u.FullName.Contains(q)));
            if (role == "brand")
                query = query.Where(u => u.Role == UserRole.Brand);
            else if (role == "creator")
                query = query.Where(u => u.Role == UserRole.Influencer);

            var total = await query.CountAsync(ct);
            var pageSize = 50;
            var skip = ((page ?? 1) - 1) * pageSize;

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip(skip).Take(pageSize)
                .Select(u => new
                {
                    u.Id, u.ClerkUserId, u.Email, u.FullName, u.AvatarUrl,
                    Role = u.Role.ToString().ToLower(),
                    u.IsBlocked, u.BlockedReason, u.BlockedAt, u.CreatedAt,
                })
                .ToListAsync(ct);

            return Results.Ok(new { total, page = page ?? 1, pageSize, items = users });
        }).WithName("SuperAdminUsers");

        // ── Block / Unblock ───────────────────────────────────────
        g.MapPost("/users/{id:guid}/block", async (Guid id, BlockBody body, IFolkieDbContext db, ICurrentUser cu, CancellationToken ct) =>
        {
            if (!cu.IsSuperAdmin) return Results.Forbid();
            var user = await db.Users.FindAsync([id], ct);
            if (user is null) return Results.NotFound();
            user.Block(body.Reason);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { blocked = true });
        }).WithName("BlockUser");

        g.MapPost("/users/{id:guid}/unblock", async (Guid id, IFolkieDbContext db, ICurrentUser cu, CancellationToken ct) =>
        {
            if (!cu.IsSuperAdmin) return Results.Forbid();
            var user = await db.Users.FindAsync([id], ct);
            if (user is null) return Results.NotFound();
            user.Unblock();
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { blocked = false });
        }).WithName("UnblockUser");

        // ── Campaigns ─────────────────────────────────────────────
        g.MapGet("/campaigns", async (string? status, int? page, IFolkieDbContext db, ICurrentUser cu, CancellationToken ct) =>
        {
            if (!cu.IsSuperAdmin) return Results.Forbid();

            var query = db.Campaigns.AsQueryable();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<CampaignStatus>(status, true, out var s))
                query = query.Where(c => c.Status == s);

            var total = await query.CountAsync(ct);
            var pageSize = 50;
            var skip = ((page ?? 1) - 1) * pageSize;

            var campaigns = await (
                from c in query.OrderByDescending(c => c.CreatedAt).Skip(skip).Take(pageSize)
                join b in db.BrandProfiles on c.BrandProfileId equals b.Id
                select new
                {
                    c.Id, c.Title, c.ProductCategory,
                    Status = System.Text.RegularExpressions.Regex.Replace(c.Status.ToString(), "([A-Z])", "_$1").TrimStart('_').ToLower(),
                    c.TotalBudget, c.PlatformFeeRate, c.InfluencerCount,
                    Commission = c.TotalBudget * c.PlatformFeeRate / 100m,
                    BrandName = b.BrandName,
                    c.ApplicationDeadline, c.PublishStartDate, c.PublishEndDate, c.CreatedAt,
                    ApplicationCount = db.CampaignApplications.Count(a => a.CampaignId == c.Id),
                    ApprovedCount = db.CampaignApplications.Count(a => a.CampaignId == c.Id && a.Status == ApplicationStatus.Approved),
                }
            ).ToListAsync(ct);

            return Results.Ok(new { total, page = page ?? 1, pageSize, items = campaigns });
        }).WithName("SuperAdminCampaigns");

        // ── Pending queues ────────────────────────────────────────
        g.MapGet("/pending/brand-payments", async (IFolkieDbContext db, ICurrentUser cu, CancellationToken ct) =>
        {
            if (!cu.IsSuperAdmin) return Results.Forbid();
            var rows = await (
                from p in db.BrandPayments.Where(p => p.Status == BrandPaymentStatus.Pending)
                join c in db.Campaigns on p.CampaignId equals c.Id
                join b in db.BrandProfiles on p.BrandProfileId equals b.Id
                orderby p.CreatedAt descending
                select new { p.Id, p.Amount, p.Status, p.CreatedAt, CampaignTitle = c.Title, BrandName = b.BrandName, p.TransferReference }
            ).ToListAsync(ct);
            return Results.Ok(rows);
        }).WithName("SuperAdminPendingBrandPayments");

        g.MapGet("/pending/creator-payouts", async (IFolkieDbContext db, ICurrentUser cu, CancellationToken ct) =>
        {
            if (!cu.IsSuperAdmin) return Results.Forbid();
            var rows = await (
                from p in db.Payments.Where(p => p.Status == PaymentStatus.Approved || p.Status == PaymentStatus.Pending)
                join c in db.Campaigns on p.CampaignId equals c.Id into cj from c in cj.DefaultIfEmpty()
                join ip in db.InfluencerProfiles on p.InfluencerProfileId equals ip.Id into ij from ip in ij.DefaultIfEmpty()
                orderby p.CreatedAt descending
                select new { p.Id, p.Amount, p.IbanName, p.Iban, Status = p.Status.ToString().ToLower(), CampaignTitle = c != null ? c.Title : null, Handle = ip != null ? ip.TiktokHandle : null, p.CreatedAt }
            ).ToListAsync(ct);
            return Results.Ok(rows);
        }).WithName("SuperAdminCreatorPayouts");

        g.MapGet("/pending/submissions", async (IFolkieDbContext db, ICurrentUser cu, CancellationToken ct) =>
        {
            if (!cu.IsSuperAdmin) return Results.Forbid();
            var rows = await (
                from s in db.ContentSubmissions.Where(s => s.Status == SubmissionStatus.Submitted)
                join a in db.CampaignApplications on s.ApplicationId equals a.Id
                join c in db.Campaigns on a.CampaignId equals c.Id
                join ip in db.InfluencerProfiles on a.InfluencerProfileId equals ip.Id
                orderby s.SubmittedAt descending
                select new { s.Id, CampaignTitle = c.Title, Handle = ip.TiktokHandle, s.Script, s.SubmittedAt }
            ).ToListAsync(ct);
            return Results.Ok(rows);
        }).WithName("SuperAdminPendingSubmissions");

        // ── Revenue breakdown ─────────────────────────────────────
        g.MapGet("/revenue", async (IFolkieDbContext db, ICurrentUser cu, CancellationToken ct) =>
        {
            if (!cu.IsSuperAdmin) return Results.Forbid();

            var campaigns = await db.Campaigns
                .Where(c => c.Status != CampaignStatus.Draft && c.Status != CampaignStatus.Cancelled)
                .Join(db.BrandProfiles, c => c.BrandProfileId, b => b.Id, (c, b) => new
                {
                    c.Id, c.Title, b.BrandName,
                    Status = System.Text.RegularExpressions.Regex.Replace(c.Status.ToString(), "([A-Z])", "_$1").TrimStart('_').ToLower(),
                    c.TotalBudget, c.PlatformFeeRate,
                    Commission = c.TotalBudget * c.PlatformFeeRate / 100m,
                    c.CreatedAt,
                })
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(ct);

            return Results.Ok(campaigns);
        }).WithName("SuperAdminRevenue");

        // ── Confirm brand payment received ────────────────────────
        g.MapPost("/brand-payments/{id:guid}/confirm", async (Guid id, ConfirmBody body, IFolkieDbContext db, ICurrentUser cu, CancellationToken ct) =>
        {
            if (!cu.IsSuperAdmin) return Results.Forbid();
            var payment = await db.BrandPayments.FindAsync([id], ct);
            if (payment is null) return Results.NotFound();
            payment.Confirm(Guid.Empty, body.Reference, null, body.Note);
            var campaign = await db.Campaigns.FindAsync([payment.CampaignId], ct);
            campaign?.Activate();
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { confirmed = true });
        }).WithName("SuperAdminConfirmBrandPayment");

        // ── Mark creator payout transferred ──────────────────────
        g.MapPost("/payments/{id:guid}/transfer", async (Guid id, TransferBody body, IFolkieDbContext db, ICurrentUser cu, CancellationToken ct) =>
        {
            if (!cu.IsSuperAdmin) return Results.Forbid();
            var payment = await db.Payments.FindAsync([id], ct);
            if (payment is null) return Results.NotFound();
            payment.MarkTransferred(body.Reference);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { transferred = true });
        }).WithName("SuperAdminTransferPayment");

        return app;
    }
}

public sealed record BlockBody(string Reason);
public sealed record TransferBody(string Reference);
public sealed record ConfirmBody(string? Reference, string? Note);
