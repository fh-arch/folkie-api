using Folkie.Application.Common.Interfaces;

namespace Folkie.Api.Endpoints;

public static class FileEndpoints
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/files")
            .WithTags("Files")
            .RequireAuthorization();

        group.MapPost("/upload-url", async (
            UploadUrlRequest req,
            IFileStorage storage,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            // Basic validation
            if (req.SizeBytes > 100 * 1024 * 1024) // 100 MB
                return Results.BadRequest(new { error = "Dosya 100MB'tan büyük olamaz." });

            var allowedTypes = new[] { "image/png", "image/jpeg", "image/webp", "video/mp4", "video/quicktime" };
            if (!allowedTypes.Contains(req.ContentType))
                return Results.BadRequest(new { error = "Desteklenmeyen dosya tipi." });

            var user = await currentUser.RequireUserAsync(ct);
            // Path: submissions/{userId}/{guid}-{filename}
            var safeFilename = string.Concat(req.Filename.Where(c => char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_'));
            var key = $"{req.Folder ?? "uploads"}/{user.Id}/{Guid.NewGuid()}-{safeFilename}";

            try
            {
                var presigned = await storage.CreateUploadUrlAsync(key, req.ContentType, req.SizeBytes, ct: ct);
                return Results.Ok(presigned);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(
                    title: ex.Message,
                    detail: "Contabo storage henüz yapılandırılmamış.",
                    statusCode: 503);
            }
        })
        .WithName("CreateUploadUrl");

        return app;
    }
}

public sealed record UploadUrlRequest(
    string Filename,
    string ContentType,
    long SizeBytes,
    string? Folder);
