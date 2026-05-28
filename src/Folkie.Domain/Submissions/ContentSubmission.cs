using Folkie.Domain.Common;

namespace Folkie.Domain.Submissions;

public class ContentSubmission : Entity
{
    public Guid ApplicationId { get; private set; }
    public string? VideoUrl { get; private set; }            // R2'deki video
    public string? ExternalVideoUrl { get; private set; }    // TikTok yayın linki
    public string? Script { get; private set; }
    public List<string> Hashtags { get; private set; } = new();
    public SubmissionStatus Status { get; private set; } = SubmissionStatus.Submitted;
    public string? RevisionNote { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }

    private ContentSubmission() { }

    public static ContentSubmission Create(Guid applicationId, string? videoUrl, string? script, IEnumerable<string> hashtags)
    {
        return new ContentSubmission
        {
            ApplicationId = applicationId,
            VideoUrl = videoUrl,
            Script = script,
            Hashtags = hashtags.ToList(),
        };
    }

    public void Approve()
    {
        Status = SubmissionStatus.Approved;
        ReviewedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void RequestRevision(string note)
    {
        Status = SubmissionStatus.RevisionRequested;
        RevisionNote = note;
        ReviewedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Publish(string externalVideoUrl)
    {
        if (Status != SubmissionStatus.Approved)
            throw new InvalidOperationException("Yalnızca onaylı içerik yayınlanabilir.");
        ExternalVideoUrl = externalVideoUrl;
        Status = SubmissionStatus.Published;
        PublishedAt = DateTimeOffset.UtcNow;
        Touch();
    }
}
