namespace Folkie.Application.Common.Interfaces;

/// <summary>
/// S3-compatible file storage abstraction.
/// Implementation: Contabo Object Storage (Sprint 5'te Contabo keys ile aktif).
/// </summary>
public interface IFileStorage
{
    /// <summary>Client'ın direkt PUT yapabileceği presigned URL üretir.</summary>
    Task<PresignedUploadDto> CreateUploadUrlAsync(
        string key,
        string contentType,
        long maxBytes,
        TimeSpan? expiresIn = null,
        CancellationToken ct = default);

    /// <summary>Public URL üretir (presigned değil, kalıcı).</summary>
    string GetPublicUrl(string key);

    /// <summary>Sil.</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);
}

public sealed record PresignedUploadDto(
    string UploadUrl,
    string Key,
    string PublicUrl,
    DateTimeOffset ExpiresAt);
