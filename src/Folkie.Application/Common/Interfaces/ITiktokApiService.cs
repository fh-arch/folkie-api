namespace Folkie.Application.Common.Interfaces;

public sealed record TiktokTokenResult(
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset ExpiresAt,
    string OpenId);

public sealed record TiktokUserInfo(
    string OpenId,
    string DisplayName,
    string? AvatarUrl,
    int FollowerCount,
    int LikesCount,
    int VideoCount);

public interface ITiktokApiService
{
    Task<TiktokTokenResult> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct = default);
    Task<TiktokUserInfo> GetUserInfoAsync(string accessToken, CancellationToken ct = default);
}
