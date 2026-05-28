using System.Net.Http.Json;
using System.Text.Json;
using Folkie.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Folkie.Infrastructure.Ai;

/// <summary>
/// Google Gemini API ile semantic matching.
///
/// API key gerekli: GEMINI_API_KEY (https://aistudio.google.com/apikey, ücretsiz)
/// Embedding: text-embedding-004 — 1500 req/dakika ücretsiz quota
/// Scorer: gemini-2.0-flash — $0.10/1M in, $0.40/1M out
///
/// API key yoksa stub döner (semantic match devre dışı).
/// </summary>
public sealed class GeminiMatchingService : IAiMatchingService
{
    private const string EmbedUrl = "https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent";
    private const string ChatUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly ILogger<GeminiMatchingService> _logger;

    public GeminiMatchingService(
        HttpClient http,
        IConfiguration configuration,
        ILogger<GeminiMatchingService> logger)
    {
        _http = http;
        _apiKey = configuration["Gemini:ApiKey"];
        _logger = logger;
    }

    public async Task<float[]> CreateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogDebug("Gemini API key tanımsız — boş embedding döndü");
            return Array.Empty<float>();
        }

        var url = $"{EmbedUrl}?key={_apiKey}";
        var payload = new
        {
            content = new
            {
                parts = new[] { new { text } }
            }
        };

        try
        {
            var res = await _http.PostAsJsonAsync(url, payload, ct);
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
            var values = json?.RootElement.GetProperty("embedding").GetProperty("values");
            if (values is null) return Array.Empty<float>();

            return values.Value.EnumerateArray()
                .Select(v => v.GetSingle())
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini embedding hatası");
            return Array.Empty<float>();
        }
    }

    public async Task<MatchReasoning> ScoreMatchAsync(
        string creatorBio,
        IEnumerable<string> creatorCategories,
        string campaignBrief,
        IEnumerable<string> campaignCategories,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return new MatchReasoning(50, "AI scorer devre dışı (Gemini API key tanımsız)");

        var prompt = $$"""
            Sen Folkie'nin AI eşleştirme algoritmasısın. Bir creator profili ve bir kampanya brief'i verildi.
            0-100 arası uyum puanı ver ve 2-3 cümle ile neden uyduğunu/uymadığını açıkla.

            Kriter:
            - Kategori uyumu (creator ilgi alanları vs kampanya ürünü)
            - Hedef kitle (creator demografisi vs kampanya hedefi)
            - Ton uyumu

            Cevabı JSON formatında ver: {"score": int, "reasoning": "string"}.

            CREATOR:
            Bio: {{creatorBio}}
            Kategoriler: {{string.Join(", ", creatorCategories)}}

            KAMPANYA:
            Brief: {{campaignBrief}}
            Kategoriler: {{string.Join(", ", campaignCategories)}}
            """;

        var url = $"{ChatUrl}?key={_apiKey}";
        var payload = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new
            {
                responseMimeType = "application/json",
                temperature = 0.3
            }
        };

        try
        {
            var res = await _http.PostAsJsonAsync(url, payload, ct);
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
            var text = json?.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrEmpty(text))
                return new MatchReasoning(50, "AI cevabı boş döndü");

            var parsed = JsonSerializer.Deserialize<JsonElement>(text);
            var score = parsed.GetProperty("score").GetInt32();
            var reasoning = parsed.GetProperty("reasoning").GetString() ?? "";
            return new MatchReasoning(Math.Clamp(score, 0, 100), reasoning);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini scorer hatası");
            return new MatchReasoning(50, "AI scorer geçici hata");
        }
    }
}
