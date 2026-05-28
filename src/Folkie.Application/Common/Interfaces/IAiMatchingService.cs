namespace Folkie.Application.Common.Interfaces;

/// <summary>
/// AI eşleştirme servisi — Gemini 2.0 Flash kullanır.
/// Şu an stub; Sprint 5'te pgvector + Gemini API key ile gerçek implementasyon.
/// </summary>
public interface IAiMatchingService
{
    /// <summary>Verilen metin için 768-dim embedding döner. Gemini text-embedding-004.</summary>
    Task<float[]> CreateEmbeddingAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Bir creator profili ile bir kampanya brief'ini karşılaştırır.
    /// 0-100 arası skor + nedeni döner.
    /// </summary>
    Task<MatchReasoning> ScoreMatchAsync(
        string creatorBio,
        IEnumerable<string> creatorCategories,
        string campaignBrief,
        IEnumerable<string> campaignCategories,
        CancellationToken ct = default);
}

public sealed record MatchReasoning(int Score, string Reasoning);
