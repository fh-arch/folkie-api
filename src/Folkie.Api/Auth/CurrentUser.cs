using System.Security.Claims;
using Folkie.Application.Common.Interfaces;
using Folkie.Domain.Common;
using Folkie.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Folkie.Api.Auth;

/// <summary>
/// Scoped — bir HTTP isteği boyunca yaşar.
/// JWT claim'lerini sync, Folkie User entity'sini async-cache'li sağlar.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IFolkieDbContext _db;
    private User? _cachedUser;
    private bool _loaded;

    public string? ClerkUserId { get; }
    public UserRole? Role { get; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(ClerkUserId);

    public bool IsSuperAdmin { get; }

    public CurrentUser(IHttpContextAccessor accessor, IFolkieDbContext db)
    {
        _db = db;

        var ctx = accessor.HttpContext;
        if (ctx is null) return;

        // Super admin bypass — API key validated in SuperAdminKeyMiddleware
        if (ctx.Items.ContainsKey("IsSuperAdmin"))
        {
            IsSuperAdmin = true;
            Role = UserRole.Admin;
            ClerkUserId = "__superadmin__";
            return;
        }

        var principal = ctx.User;
        if (principal?.Identity?.IsAuthenticated != true) return;

        ClerkUserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;

        var roleClaim = principal.FindFirst("folkie_role")?.Value
            ?? principal.FindFirst(ClaimTypes.Role)?.Value;
        if (Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var role))
            Role = role;
    }

    public async Task<User?> GetUserAsync(CancellationToken ct = default)
    {
        if (IsSuperAdmin) return null; // super admin has no DB user
        if (_loaded) return _cachedUser;
        if (string.IsNullOrEmpty(ClerkUserId))
        {
            _loaded = true;
            return null;
        }

        _cachedUser = await _db.Users
            .FirstOrDefaultAsync(u => u.ClerkUserId == ClerkUserId, ct);
        _loaded = true;
        return _cachedUser;
    }

    public async Task<User> RequireUserAsync(CancellationToken ct = default)
    {
        if (IsSuperAdmin)
            throw new InvalidOperationException("Super admin context — use IsSuperAdmin check instead.");
        var user = await GetUserAsync(ct);
        if (user is null)
            throw new UnauthorizedAccessException("Folkie kullanıcı kaydı bulunamadı.");
        if (user.IsBlocked)
            throw new UnauthorizedAccessException("Bu hesap askıya alınmıştır.");
        return user;
    }
}
