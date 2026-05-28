using Folkie.Domain.Common;

namespace Folkie.Domain.Payments;

/// <summary>Folkie → Influencer manuel ödemesi.</summary>
public class Payment : Entity
{
    public Guid CampaignId { get; private set; }
    public Guid InfluencerProfileId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentType PaymentType { get; private set; } = PaymentType.Base;
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public EncryptedString Iban { get; private set; } = EncryptedString.Empty;
    public string IbanName { get; private set; } = string.Empty;
    public string? AdminNote { get; private set; }
    public string? TransferReference { get; private set; }
    public Guid? ApprovedById { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? TransferredAt { get; private set; }

    private Payment() { }

    public static Payment Create(
        Guid campaignId,
        Guid influencerProfileId,
        decimal amount,
        EncryptedString iban,
        string ibanName,
        PaymentType type = PaymentType.Base)
    {
        return new Payment
        {
            CampaignId = campaignId,
            InfluencerProfileId = influencerProfileId,
            Amount = amount,
            Iban = iban,
            IbanName = ibanName,
            PaymentType = type,
        };
    }

    public void Approve(Guid adminUserId, string? note = null)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Sadece bekleyen ödeme onaylanabilir.");
        Status = PaymentStatus.Approved;
        ApprovedById = adminUserId;
        ApprovedAt = DateTimeOffset.UtcNow;
        AdminNote = note;
        Touch();
    }

    public void MarkTransferred(string reference, string? note = null)
    {
        if (Status != PaymentStatus.Approved)
            throw new InvalidOperationException("Sadece onaylı ödeme transfer edilebilir.");
        Status = PaymentStatus.Transferred;
        TransferReference = reference;
        TransferredAt = DateTimeOffset.UtcNow;
        if (note is not null) AdminNote = note;
        Touch();
    }

    public void MarkFailed(string note)
    {
        Status = PaymentStatus.Failed;
        AdminNote = note;
        Touch();
    }
}
