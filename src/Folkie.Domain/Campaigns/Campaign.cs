using Folkie.Domain.Common;

namespace Folkie.Domain.Campaigns;

public class Campaign : Entity
{
    public Guid BrandProfileId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public string ProductCategory { get; private set; } = string.Empty;
    public string Brief { get; private set; } = string.Empty;
    public List<string> RequiredHashtags { get; private set; } = new();
    public List<ContentType> ContentTypes { get; private set; } = new() { ContentType.Video };
    public string? Tone { get; private set; }
    public List<string> TargetCategories { get; private set; } = new();
    public List<string> TargetCities { get; private set; } = new();
    public List<string> ContentLanguage { get; private set; } = new() { "tr" };
    public int MinFollowers { get; private set; } = 1000;
    public int MaxFollowers { get; private set; } = 10_000;
    public int InfluencerCount { get; private set; }
    public decimal BudgetPerInfluencer { get; private set; }
    public decimal PlatformFeeRate { get; private set; } = 15.0m;
    public ProductDelivery ProductDelivery { get; private set; } = ProductDelivery.None;
    public ApprovalMode ApprovalMode { get; private set; } = ApprovalMode.Manual;
    public DateOnly ApplicationDeadline { get; private set; }
    public DateOnly PublishStartDate { get; private set; }
    public DateOnly PublishEndDate { get; private set; }
    public bool IsFlashCampaign { get; private set; }
    public TimeOnly? FlashPublishTime { get; private set; }
    public CampaignStatus Status { get; private set; } = CampaignStatus.Draft;

    /// <summary>Otomatik hesaplanan toplam bütçe.</summary>
    public decimal TotalBudget => InfluencerCount * BudgetPerInfluencer;

    private Campaign() { }

    public static Campaign CreateDraft(
        Guid brandProfileId,
        string title,
        string productName,
        string productCategory,
        string brief,
        int influencerCount,
        decimal budgetPerInfluencer,
        DateOnly applicationDeadline,
        DateOnly publishStartDate,
        DateOnly publishEndDate)
    {
        return new Campaign
        {
            BrandProfileId = brandProfileId,
            Title = title,
            ProductName = productName,
            ProductCategory = productCategory,
            Brief = brief,
            InfluencerCount = influencerCount,
            BudgetPerInfluencer = budgetPerInfluencer,
            ApplicationDeadline = applicationDeadline,
            PublishStartDate = publishStartDate,
            PublishEndDate = publishEndDate,
            Status = CampaignStatus.Draft,
        };
    }

    public void UpdateBasics(
        string title,
        string productName,
        string productCategory,
        string brief,
        IEnumerable<string> requiredHashtags,
        IEnumerable<ContentType> contentTypes,
        string? tone)
    {
        EnsureDraft();
        Title = title;
        ProductName = productName;
        ProductCategory = productCategory;
        Brief = brief;
        RequiredHashtags = requiredHashtags.ToList();
        ContentTypes = contentTypes.ToList();
        Tone = tone;
        Touch();
    }

    public void UpdateTargeting(
        IEnumerable<string> categories,
        IEnumerable<string> cities,
        IEnumerable<string> languages,
        int minFollowers,
        int maxFollowers)
    {
        EnsureDraft();
        TargetCategories = categories.ToList();
        TargetCities = cities.ToList();
        ContentLanguage = languages.ToList();
        MinFollowers = minFollowers;
        MaxFollowers = maxFollowers;
        Touch();
    }

    public void UpdateBudget(int influencerCount, decimal budgetPerInfluencer)
    {
        EnsureDraft();
        InfluencerCount = influencerCount;
        BudgetPerInfluencer = budgetPerInfluencer;
        Touch();
    }

    public void SetFlashCampaign(TimeOnly publishTime)
    {
        EnsureDraft();
        IsFlashCampaign = true;
        FlashPublishTime = publishTime;
        Touch();
    }

    public void SubmitForPayment()
    {
        EnsureDraft();
        Status = CampaignStatus.PendingPayment;
        Touch();
    }

    public void Activate()
    {
        if (Status != CampaignStatus.PendingPayment)
            throw new InvalidOperationException("Sadece ödeme bekleyen kampanya aktive edilebilir.");
        Status = CampaignStatus.Active;
        Touch();
    }

    public void CloseApplications() { Status = CampaignStatus.ApplicationsClosed; Touch(); }
    public void StartProgress() { Status = CampaignStatus.InProgress; Touch(); }
    public void Complete() { Status = CampaignStatus.Completed; Touch(); }
    public void Cancel() { Status = CampaignStatus.Cancelled; Touch(); }

    private void EnsureDraft()
    {
        if (Status != CampaignStatus.Draft)
            throw new InvalidOperationException("Bu işlem yalnızca taslak kampanyada yapılabilir.");
    }
}
