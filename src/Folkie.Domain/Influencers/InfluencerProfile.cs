using Folkie.Domain.Common;

namespace Folkie.Domain.Influencers;

public class InfluencerProfile : Entity
{
    public Guid UserId { get; private set; }
    public string? TiktokHandle { get; private set; }
    public string? TiktokUserId { get; private set; }
    public int FollowerCount { get; private set; }
    public decimal EngagementRate { get; private set; }
    public string? City { get; private set; }
    public string Country { get; private set; } = "TR";
    public List<string> ContentLanguage { get; private set; } = new() { "tr" };
    public List<string> Categories { get; private set; } = new();
    public List<string> Subcategories { get; private set; } = new();
    public string? Bio { get; private set; }
    public EncryptedString? Iban { get; private set; }
    public string? IbanName { get; private set; }
    public bool IsVerified { get; private set; }
    public bool IsActive { get; private set; } = true;
    public decimal? FakeFollowerScore { get; private set; }
    public DateTimeOffset? LastTiktokSync { get; private set; }

    // TikTok OAuth tokens (şifreli saklanır)
    public EncryptedString? TiktokAccessToken { get; private set; }
    public EncryptedString? TiktokRefreshToken { get; private set; }
    public DateTimeOffset? TiktokTokenExpiresAt { get; private set; }
    public int? TiktokLikesCount { get; private set; }
    public int? TiktokVideoCount { get; private set; }
    public string? TiktokAvatarUrl { get; private set; }

    /// <summary>Tier follower_count'tan domain'de hesaplanır (DB'de kolon yok).</summary>
    public InfluencerTier Tier => FollowerCount switch
    {
        < 10_000 => InfluencerTier.Nano,
        < 100_000 => InfluencerTier.Micro,
        _ => InfluencerTier.MidTier,
    };

    private InfluencerProfile() { }

    public static InfluencerProfile Create(Guid userId, string? city, IEnumerable<string> categories)
    {
        return new InfluencerProfile
        {
            UserId = userId,
            City = city,
            Categories = categories.ToList(),
        };
    }

    public void UpdateBio(string? bio, string? city, IEnumerable<string> categories, IEnumerable<string> subcategories)
    {
        Bio = bio;
        City = city;
        Categories = categories.ToList();
        Subcategories = subcategories.ToList();
        Touch();
    }

    public void ConnectTiktok(string handle, string tiktokUserId, int followers, decimal engagement)
    {
        TiktokHandle = handle;
        TiktokUserId = tiktokUserId;
        FollowerCount = followers;
        EngagementRate = engagement;
        LastTiktokSync = DateTimeOffset.UtcNow;
        Touch();
    }

    public void RefreshTiktokStats(int followers, decimal engagement)
    {
        FollowerCount = followers;
        EngagementRate = engagement;
        LastTiktokSync = DateTimeOffset.UtcNow;
        Touch();
    }

    public void SetIban(EncryptedString iban, string ibanName)
    {
        Iban = iban;
        IbanName = ibanName;
        Touch();
    }

    public void Verify() { IsVerified = true; Touch(); }
    public void Deactivate() { IsActive = false; Touch(); }
    public void Activate() { IsActive = true; Touch(); }
    public void StoreTiktokOAuth(
        EncryptedString accessToken,
        EncryptedString? refreshToken,
        DateTimeOffset? expiresAt,
        string? avatarUrl,
        int? likesCount,
        int? videoCount)
    {
        TiktokAccessToken = accessToken;
        TiktokRefreshToken = refreshToken;
        TiktokTokenExpiresAt = expiresAt;
        TiktokAvatarUrl = avatarUrl;
        TiktokLikesCount = likesCount;
        TiktokVideoCount = videoCount;
        Touch();
    }

    public void SetFakeFollowerScore(decimal score) { FakeFollowerScore = score; Touch(); }
}
