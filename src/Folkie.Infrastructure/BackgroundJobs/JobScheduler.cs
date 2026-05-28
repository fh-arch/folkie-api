using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Folkie.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire ile çalışan otomatik işler. Program.cs'te `RegisterRecurringJobs()` çağrılır.
/// </summary>
public sealed class JobScheduler
{
    public static void RegisterRecurringJobs(IServiceProvider sp)
    {
        // 1. Daily TikTok stats sync (placeholder — TikTok API Sprint 5'te)
        RecurringJob.AddOrUpdate<FolkieJobs>(
            "tiktok-stats-sync",
            j => j.SyncTiktokStatsAsync(CancellationToken.None),
            "0 3 * * *", // Her gece 03:00
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        // 2. Süresi geçen kampanyaları kapat (deadline geçen → applications_closed)
        RecurringJob.AddOrUpdate<FolkieJobs>(
            "close-expired-campaigns",
            j => j.CloseExpiredCampaignsAsync(CancellationToken.None),
            "*/15 * * * *"); // 15 dakikada bir

        // 3. Yayın bitiş tarihi geçen kampanyaları tamamla
        RecurringJob.AddOrUpdate<FolkieJobs>(
            "mark-completed-campaigns",
            j => j.MarkCompletedCampaignsAsync(CancellationToken.None),
            "0 1 * * *"); // Her gece 01:00

        // 4. Supabase keepalive (free tier 7-gün pause önler)
        RecurringJob.AddOrUpdate<FolkieJobs>(
            "supabase-keepalive",
            j => j.PingDatabaseAsync(CancellationToken.None),
            "0 6 * * *"); // Her gün 06:00
    }
}

public sealed class FolkieJobs
{
    private readonly IFolkieDbContext _db;
    private readonly ILogger<FolkieJobs> _logger;

    public FolkieJobs(IFolkieDbContext db, ILogger<FolkieJobs> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SyncTiktokStatsAsync(CancellationToken ct)
    {
        // Sprint 5: TikTok Business API'den her aktif creator için
        // takipçi sayısı, son 20 video metrik, engagement oranı çek
        _logger.LogInformation("TikTok stats sync placeholder — Sprint 5'te implement edilecek");
        await Task.CompletedTask;
    }

    public async Task CloseExpiredCampaignsAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expired = await _db.Campaigns
            .Where(c => c.Status == CampaignStatus.Active
                && c.ApplicationDeadline < today)
            .ToListAsync(ct);

        foreach (var c in expired)
        {
            c.CloseApplications();
            _logger.LogInformation("Kampanya başvuruları kapatıldı: {Id} {Title}", c.Id, c.Title);
        }

        if (expired.Count > 0) await _db.SaveChangesAsync(ct);
    }

    public async Task MarkCompletedCampaignsAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var ready = await _db.Campaigns
            .Where(c => (c.Status == CampaignStatus.ApplicationsClosed
                    || c.Status == CampaignStatus.InProgress)
                && c.PublishEndDate < today)
            .ToListAsync(ct);

        foreach (var c in ready)
        {
            c.Complete();
            _logger.LogInformation("Kampanya tamamlandı: {Id} {Title}", c.Id, c.Title);
        }

        if (ready.Count > 0) await _db.SaveChangesAsync(ct);
    }

    public async Task PingDatabaseAsync(CancellationToken ct)
    {
        // Supabase free tier 7 günde 1 hareket istemese pause olur.
        // Basit bir SELECT 1 yeterli.
        var _ = await _db.Users.AnyAsync(ct);
        _logger.LogDebug("Supabase keepalive ping ok");
    }
}
