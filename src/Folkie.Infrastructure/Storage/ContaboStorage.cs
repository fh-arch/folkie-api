using Amazon.S3;
using Amazon.S3.Model;
using Folkie.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Folkie.Infrastructure.Storage;

/// <summary>
/// Contabo Object Storage (S3-compatible) wrapper.
/// Endpoint: https://eu2.contabostorage.com (or region-specific)
/// Forces path-style addressing because Contabo doesn't support virtual-hosted style.
/// </summary>
public sealed class ContaboStorage : IFileStorage, IDisposable
{
    private readonly IAmazonS3? _s3;
    private readonly string? _bucket;
    private readonly string? _publicUrl;
    private readonly ILogger<ContaboStorage> _logger;
    private bool _disposed;

    public ContaboStorage(IConfiguration configuration, ILogger<ContaboStorage> logger)
    {
        _logger = logger;

        var endpoint = configuration["ContaboStorage:Endpoint"];
        var accessKey = configuration["ContaboStorage:AccessKeyId"];
        var secretKey = configuration["ContaboStorage:SecretAccessKey"];
        _bucket = configuration["ContaboStorage:BucketName"];
        _publicUrl = configuration["ContaboStorage:PublicUrl"];

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
        {
            _logger.LogInformation("Contabo storage credentials yok — file upload devre dışı");
            return;
        }

        _s3 = new AmazonS3Client(accessKey, secretKey, new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            SignatureVersion = "4",
        });
    }

    public async Task<PresignedUploadDto> CreateUploadUrlAsync(
        string key,
        string contentType,
        long maxBytes,
        TimeSpan? expiresIn = null,
        CancellationToken ct = default)
    {
        if (_s3 is null || string.IsNullOrEmpty(_bucket))
            throw new InvalidOperationException("Contabo storage yapılandırılmamış.");

        var expires = DateTime.UtcNow.Add(expiresIn ?? TimeSpan.FromMinutes(15));
        var req = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Expires = expires,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
        };

        var url = await _s3.GetPreSignedURLAsync(req);
        return new PresignedUploadDto(url, key, GetPublicUrl(key), expires);
    }

    public string GetPublicUrl(string key) =>
        string.IsNullOrEmpty(_publicUrl) ? $"/{key}" : $"{_publicUrl}/{key}";

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        if (_s3 is null || string.IsNullOrEmpty(_bucket)) return;
        await _s3.DeleteObjectAsync(_bucket, key, ct);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _s3?.Dispose();
        _disposed = true;
    }
}
