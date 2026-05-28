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

    public CurrentUser(IHttpContextAccessor accessor, IFolkieDbContext db)
    {
        _db = db;

        var principal = accessor.HttpContext?.User;
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
        var user = await GetUserAsync(ct);
        return user ?? throw new UnauthorizedAccessException(
            "Folkie kullanıcı kaydı bulunamadı. Webhook çalışmamış olabilir.");
    }
}
