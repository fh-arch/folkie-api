using Folkie.Domain.Common;

namespace Folkie.Domain.Payments;

/// <summary>Marka → Folkie kampanya ödemesi.</summary>
public class BrandPayment : Entity
{
    public Guid CampaignId { get; private set; }
    public Guid BrandProfileId { get; private set; }
    public decimal Amount { get; private set; }
    public BrandPaymentStatus Status { get; private set; } = BrandPaymentStatus.Pending;
    public string PaymentMethod { get; private set; } = "bank_transfer";
    public string? TransferReference { get; private set; }
    public string? ReceiptUrl { get; private set; }
    public string? AdminNote { get; private set; }
    public Guid? ConfirmedById { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }

    private BrandPayment() { }

    public static BrandPayment Create(Guid campaignId, Guid brandProfileId, decimal amount)
    {
        return new BrandPayment
        {
            CampaignId = campaignId,
            BrandProfileId = brandProfileId,
            Amount = amount,
        };
    }

    public void Confirm(Guid adminUserId, string? transferReference, string? receiptUrl, string? note = null)
    {
        Status = BrandPaymentStatus.Received;
        TransferReference = transferReference;
        ReceiptUrl = receiptUrl;
        ConfirmedById = adminUserId;
        ConfirmedAt = DateTimeOffset.UtcNow;
        AdminNote = note;
        Touch();
    }

    public void MarkPartial(string note)
    {
        Status = BrandPaymentStatus.Partial;
        AdminNote = note;
        Touch();
    }

    public void MarkFailed(string note)
    {
        Status = BrandPaymentStatus.Failed;
        AdminNote = note;
        Touch();
    }
}
