using System.Net;
using System.Text.Json;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using Folkie.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Svix;

namespace Folkie.Api.Webhooks;

public static class ClerkWebhookEndpoint
{
    public static IEndpointRouteBuilder MapClerkWebhook(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/webhooks/clerk", async (
            HttpRequest request,
            IFolkieDbContext db,
            IConfiguration configuration,
            ILogger<ClerkWebhookMarker> logger,
            CancellationToken ct) =>
        {
            // 1. Body'yi oku
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync(ct);

            // 2. Svix imzasını doğrula
            var webhookSecret = configuration["Clerk:WebhookSecret"];
            if (string.IsNullOrEmpty(webhookSecret))
            {
                logger.LogError("Clerk webhook secret yapılandırılmamış");
                return Results.StatusCode(500);
            }

            try
            {
                var svixHeaders = new WebHeaderCollection
                {
                    { "svix-id", request.Headers["svix-id"].ToString() },
                    { "svix-timestamp", request.Headers["svix-timestamp"].ToString() },
                    { "svix-signature", request.Headers["svix-signature"].ToString() },
                };
                var webhook = new Webhook(webhookSecret);
                webhook.Verify(body, svixHeaders);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Clerk webhook imza doğrulaması başarısız");
                return Results.Unauthorized();
            }

            // 3. Event'i parse et
            var doc = JsonDocument.Parse(body);
            var eventType = doc.RootElement.GetProperty("type").GetString();
            var data = doc.RootElement.GetProperty("data");

            switch (eventType)
            {
                case "user.created":
                case "user.updated":
                    await UpsertUser(db, data, logger, ct);
                    break;
                case "user.deleted":
                    await DeleteUser(db, data, logger, ct);
                    break;
                default:
                    logger.LogInformation("Clerk webhook: işlenmeyen event tipi {Type}", eventType);
                    break;
            }

            return Results.Ok();
        })
        .WithName("ClerkWebhook")
        .WithTags("Webhooks")
        .AllowAnonymous();

        return app;
    }

    private static async Task UpsertUser(
        IFolkieDbContext db,
        JsonElement data,
        ILogger logger,
        CancellationToken ct)
    {
        var clerkUserId = data.GetProperty("id").GetString()!;

        // Email: try email_addresses first, then external_accounts (Google OAuth)
        string email = string.Empty;
        if (data.TryGetProperty("email_addresses", out var emails) && emails.GetArrayLength() > 0)
        {
            var first = emails[0];
            if (first.TryGetProperty("email_address", out var emailProp))
                email = emailProp.GetString() ?? string.Empty;
        }
        if (string.IsNullOrEmpty(email) &&
            data.TryGetProperty("external_accounts", out var extAccounts) &&
            extAccounts.GetArrayLength() > 0)
        {
            var extFirst = extAccounts[0];
            if (extFirst.TryGetProperty("email_address", out var extEmail))
                email = extEmail.GetString() ?? string.Empty;
        }
        if (string.IsNullOrEmpty(email))
        {
            logger.LogWarning("Clerk webhook payload eksik email (test payload olabilir): {ClerkId}", clerkUserId);
            return;
        }

        var firstName = data.TryGetProperty("first_name", out var fn) ? fn.GetString() : null;
        var lastName = data.TryGetProperty("last_name", out var ln) ? ln.GetString() : null;
        var fullName = $"{firstName} {lastName}".Trim();
        var avatarUrl = data.TryGetProperty("image_url", out var iu) ? iu.GetString() : null;

        // Role: from publicMetadata.role; if not set yet (new signup), preserve existing DB role
        UserRole? roleFromWebhook = null;
        if (data.TryGetProperty("public_metadata", out var pm) &&
            pm.TryGetProperty("role", out var rc) &&
            Enum.TryParse<UserRole>(rc.GetString(), ignoreCase: true, out var parsed))
        {
            roleFromWebhook = parsed;
        }

        var existing = await db.Users.FirstOrDefaultAsync(u => u.ClerkUserId == clerkUserId, ct);
        if (existing is null)
        {
            // New signup: role defaults to Influencer; onboarding will fire user.updated with the real role
            var user = User.Create(clerkUserId, email, roleFromWebhook ?? UserRole.Influencer, fullName, avatarUrl);
            db.Users.Add(user);
            logger.LogInformation("Clerk user {ClerkId} oluşturuldu (Folkie {Id}, role={Role})", clerkUserId, user.Id, user.Role);
        }
        else
        {
            existing.UpdateEmail(email);
            existing.UpdateProfile(fullName, avatarUrl);
            // Only update role if Clerk sent one — don't overwrite with null/default
            if (roleFromWebhook.HasValue)
                existing.ChangeRole(roleFromWebhook.Value);
            logger.LogInformation("Clerk user {ClerkId} güncellendi (role={Role})", clerkUserId, existing.Role);
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task DeleteUser(
        IFolkieDbContext db,
        JsonElement data,
        ILogger logger,
        CancellationToken ct)
    {
        var clerkUserId = data.GetProperty("id").GetString()!;
        var existing = await db.Users.FirstOrDefaultAsync(u => u.ClerkUserId == clerkUserId, ct);
        if (existing is not null)
        {
            db.Users.Remove(existing);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Clerk user {ClerkId} silindi", clerkUserId);
        }
    }
}

internal sealed class ClerkWebhookMarker { }
