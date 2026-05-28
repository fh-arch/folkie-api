using Folkie.Domain.Common;

namespace Folkie.Domain.Users;

public class User : Entity
{
    public string ClerkUserId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public string? FullName { get; private set; }
    public string? AvatarUrl { get; private set; }

    private User() { }

    public static User Create(string clerkUserId, string email, UserRole role, string? fullName = null, string? avatarUrl = null)
    {
        return new User
        {
            ClerkUserId = clerkUserId,
            Email = email,
            Role = role,
            FullName = fullName,
            AvatarUrl = avatarUrl,
        };
    }

    public void UpdateProfile(string? fullName, string? avatarUrl)
    {
        FullName = fullName;
        AvatarUrl = avatarUrl;
        Touch();
    }

    public void ChangeRole(UserRole role)
    {
        Role = role;
        Touch();
    }

    public void UpdateEmail(string email)
    {
        Email = email;
        Touch();
    }
}
