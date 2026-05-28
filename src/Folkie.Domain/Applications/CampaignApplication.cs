using Folkie.Domain.Common;

namespace Folkie.Domain.Applications;

public class CampaignApplication : Entity
{
    public Guid CampaignId { get; private set; }
    public Guid InfluencerProfileId { get; private set; }
    public ApplicationStatus Status { get; private set; } = ApplicationStatus.Pending;
    public string? RejectionReason { get; private set; }
    public DateTimeOffset AppliedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; private set; }

    private CampaignApplication() { }

    public static CampaignApplication Create(Guid campaignId, Guid influencerProfileId)
    {
        return new CampaignApplication
        {
            CampaignId = campaignId,
            InfluencerProfileId = influencerProfileId,
        };
    }

    public void Approve()
    {
        EnsurePending();
        Status = ApplicationStatus.Approved;
        ReviewedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Reject(string reason)
    {
        EnsurePending();
        Status = ApplicationStatus.Rejected;
        RejectionReason = reason;
        ReviewedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Withdraw()
    {
        if (Status is ApplicationStatus.Approved or ApplicationStatus.Rejected)
            throw new InvalidOperationException("İncelenmiş başvuru geri çekilemez.");
        Status = ApplicationStatus.Withdrawn;
        Touch();
    }

    private void EnsurePending()
    {
        if (Status != ApplicationStatus.Pending)
            throw new InvalidOperationException("Sadece bekleyen başvurular incelenebilir.");
    }
}
