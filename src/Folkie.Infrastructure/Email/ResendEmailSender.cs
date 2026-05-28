using System.Net.Http.Json;
using Folkie.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Folkie.Infrastructure.Email;

/// <summary>
/// Resend (resend.com) ile transactional e-posta.
/// Free tier: 3K e-posta/ay, 100/gün.
/// </summary>
public sealed class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly string _fromEmail;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(
        HttpClient http,
        IConfiguration configuration,
        ILogger<ResendEmailSender> logger)
    {
        _http = http;
        _apiKey = configuration["Resend:ApiKey"];
        _fromEmail = configuration["Resend:FromEmail"] ?? "Folkie <onboarding@resend.dev>";
        _logger = logger;
    }

    public async Task<bool> SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogDebug("Resend API key tanımsız — e-posta atlandı");
            return false;
        }

        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Headers = { { "Authorization", $"Bearer {_apiKey}" } },
            Content = JsonContent.Create(new
            {
                from = _fromEmail,
                to = new[] { to },
                subject,
                html = htmlBody,
            }),
        };

        try
        {
            var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Resend hata: {Status} {Body}", res.StatusCode, body);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Resend e-posta hatası");
            return false;
        }
    }
}
