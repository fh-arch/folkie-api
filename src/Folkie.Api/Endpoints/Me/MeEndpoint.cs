using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Api.Endpoints.Me;

public static class MeEndpoint
{
    public static IEndpointRouteBuilder MapMe(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/me", [Authorize] async (
            ICurrentUser currentUser,
            IFolkieDbContext db,
            CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.ClerkUserId is null)
                return Results.Unauthorized();

            var user = await db.Users
                .Where(u => u.ClerkUserId == currentUser.ClerkUserId)
                .Select(u => new MeResponse(
                    u.Id,
                    u.ClerkUserId,
                    u.Email,
                    u.Role.ToString().ToLowerInvariant(),
                    u.FullName,
                    u.AvatarUrl,
                    u.CreatedAt))
                .FirstOrDefaultAsync(ct);

            if (user is null) return Results.NotFound(new { message = "Henüz Folkie kaydı yok. Webhook'un çalıştığından emin ol." });
            return Results.Ok(user);
        })
        .WithName("Me")
        .WithTags("User");

        // Called from onboarding immediately after Clerk metadata update,
        // before the async webhook has a chance to update the DB role.
        app.MapPut("/api/v1/me/role", [Authorize] async (
            SetRoleRequest body,
            ICurrentUser currentUser,
            IFolkieDbContext db,
            CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.ClerkUserId is null)
                return Results.Unauthorized();

            if (!Enum.TryParse<UserRole>(body.Role, ignoreCase: true, out var role))
                return Results.BadRequest(new { message = "Geçersiz rol. 'brand' veya 'influencer' olmalı." });

            var user = await db.Users.FirstOrDefaultAsync(u => u.ClerkUserId == currentUser.ClerkUserId, ct);
            if (user is null)
            {
                // User not in DB yet (webhook hasn't fired). Create a minimal record.
                user = Folkie.Domain.Users.User.Create(currentUser.ClerkUserId, string.Empty, role, null, null);
                db.Users.Add(user);
            }
            else
            {
                user.ChangeRole(role);
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(new { role = user.Role.ToString().ToLowerInvariant() });
        })
        .WithName("SetMyRole")
        .WithTags("User");

        return app;
    }
}

public sealed record SetRoleRequest(string Role);

public sealed record MeResponse(
    Guid Id,
    string ClerkUserId,
    string Email,
    string Role,
    string? FullName,
    string? AvatarUrl,
    DateTimeOffset CreatedAt);
