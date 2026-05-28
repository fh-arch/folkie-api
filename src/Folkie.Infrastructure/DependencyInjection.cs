using Folkie.Application.Common.Interfaces;
using Folkie.Infrastructure.Persistence;
using Folkie.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Folkie.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // PostgreSQL + EF Core
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres tanımlı değil.");

        services.AddDbContext<FolkieDbContext>(options =>
            options.UseNpgsql(connectionString, npg => npg.MigrationsAssembly(typeof(FolkieDbContext).Assembly.FullName)));

        services.AddScoped<IFolkieDbContext>(sp => sp.GetRequiredService<FolkieDbContext>());

        // IBAN şifreleme — Data Protection
        var keyRingPath = configuration["DataProtection:KeyRingPath"] ?? "./keys";
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath))
            .SetApplicationName("Folkie");

        services.AddSingleton<IIbanProtector, IbanProtector>();
        services.AddScoped<INotificationService, Notifications.NotificationService>();
        services.AddScoped<Notifications.EmailDispatcher>();
        services.AddScoped<BackgroundJobs.FolkieJobs>();

        // Gemini AI matching (GEMINI_API_KEY tanımlıysa devreye girer)
        services.AddHttpClient<IAiMatchingService, Ai.GeminiMatchingService>();

        // Resend transactional email
        services.AddHttpClient<IEmailSender, Email.ResendEmailSender>();

        // Contabo Object Storage (S3-compatible, ContaboStorage.* config'i tanımlıysa devreye girer)
        services.AddSingleton<IFileStorage, Storage.ContaboStorage>();

        // TikTok OAuth
        services.AddHttpClient<ITiktokApiService, TikTok.TiktokApiService>();

        return services;
    }
}
