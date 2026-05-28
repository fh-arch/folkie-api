using Folkie.Domain.Common;

namespace Folkie.Domain.Reviews;

public class Review : Entity
{
    public Guid CampaignId { get; private set; }
    public Guid ReviewerId { get; private set; }
    public Guid RevieweeId { get; private set; }
    public ReviewerRole ReviewerRole { get; private set; }
    public int Score { get; private set; }
    public string? Comment { get; private set; }

    private Review() { }

    public static Review Create(
        Guid campaignId,
        Guid reviewerId,
        Guid revieweeId,
        ReviewerRole reviewerRole,
        int score,
        string? comment = null)
    {
        if (score is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(score), "Puan 1-5 arasında olmalı.");

        return new Review
        {
            CampaignId = campaignId,
            ReviewerId = reviewerId,
            RevieweeId = revieweeId,
            ReviewerRole = reviewerRole,
            Score = score,
            Comment = comment,
        };
    }
}
