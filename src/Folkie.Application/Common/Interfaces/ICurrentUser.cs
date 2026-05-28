using Folkie.Domain.Common;
using Folkie.Domain.Users;

namespace Folkie.Application.Common.Interfaces;

/// <summary>
/// Mevcut HTTP isteğindeki kullanıcı bilgisi.
/// JWT claim'leri sync olarak hemen erişilebilir; Folkie User entity'si async DB lookup ile.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Clerk'in subject claim'i (kullanıcının Clerk ID'si).</summary>
    string? ClerkUserId { get; }

    /// <summary>JWT'deki folkie_role claim'i (publicMetadata.role'den).</summary>
    UserRole? Role { get; }

    bool IsAuthenticated { get; }

    /// <summary>
    /// Folkie DB'sinden kullanıcıyı yükler. Kayıt yoksa null döner.
    /// Aynı request içinde cache'lenir.
    /// </summary>
    Task<User?> GetUserAsync(CancellationToken ct = default);

    /// <summary>
    /// Folkie DB'sinden kullanıcıyı yükler. Kayıt yoksa exception fırlatır.
    /// Onboarding tamamlanmış kullanıcı endpoint'leri için kullanılır.
    /// </summary>
    Task<User> RequireUserAsync(CancellationToken ct = default);
}
