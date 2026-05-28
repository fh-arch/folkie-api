using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Folkie.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Folkie.Infrastructure.TikTok;

public sealed class TiktokApiService : ITiktokApiService
{
    private readonly HttpClient _http;
    private readonly string _clientKey;
    private readonly string _clientSecret;
    private readonly ILogger<TiktokApiService> _logger;

    public TiktokApiService(HttpClient http, IConfiguration config, ILogger<TiktokApiService> logger)
    {
        _http = http;
        _clientKey = config["TikTok:ClientKey"] ?? throw new InvalidOperationException("TikTok:ClientKey tanımlı değil.");
        _clientSecret = config["TikTok:ClientSecret"] ?? throw new InvalidOperationException("TikTok:ClientSecret tanımlı değil.");
        _logger = logger;
    }

    public async Task<TiktokTokenResult> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct = default)
    {
        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_key"] = _clientKey,
            ["client_secret"] = _clientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri,
        });

        var res = await _http.PostAsync("https://open.tiktokapis.com/v2/oauth/token/", body, ct);
        var raw = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
        {
            _logger.LogError("TikTok token exchange failed: {Status} {Body}", res.StatusCode, raw);
            throw new InvalidOperationException($"TikTok token alınamadı: {raw}");
        }

        var json = JsonDocument.Parse(raw).RootElement;
        var accessToken = json.GetProperty("access_token").GetString()!;
        var refreshToken = json.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var expiresIn = json.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 86400;
        var openId = json.GetProperty("open_id").GetString()!;

        return new TiktokTokenResult(
            accessToken,
            refreshToken,
            DateTimeOffset.UtcNow.AddSeconds(expiresIn),
            openId);
    }

    public async Task<TiktokUserInfo> GetUserInfoAsync(string accessToken, CancellationToken ct = default)
    {
        const string fields = "open_id,display_name,avatar_url,follower_count,likes_count,video_count";
        var req = new HttpRequestMessage(HttpMethod.Get,
            $"https://open.tiktokapis.com/v2/user/info/?fields={fields}");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var res = await _http.SendAsync(req, ct);
        var raw = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
        {
            _logger.LogError("TikTok user info failed: {Status} {Body}", res.StatusCode, raw);
            throw new InvalidOperationException($"TikTok kullanıcı bilgisi alınamadı: {raw}");
        }

        var root = JsonDocument.Parse(raw).RootElement;
        var user = root.GetProperty("data").GetProperty("user");

        return new TiktokUserInfo(
            user.GetProperty("open_id").GetString()!,
            user.GetProperty("display_name").GetString() ?? "unknown",
            user.TryGetProperty("avatar_url", out var av) ? av.GetString() : null,
            user.TryGetProperty("follower_count", out var fc) ? fc.GetInt32() : 0,
            user.TryGetProperty("likes_count", out var lc) ? lc.GetInt32() : 0,
            user.TryGetProperty("video_count", out var vc) ? vc.GetInt32() : 0);
    }
}
