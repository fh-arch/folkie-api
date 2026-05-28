namespace Folkie.Domain.Common;

public enum UserRole
{
    Influencer = 1,
    Brand = 2,
    Admin = 3,
}

public enum InfluencerTier
{
    Nano = 1,        // 1.000 - 10.000
    Micro = 2,       // 10.000 - 100.000
    MidTier = 3,     // 100.000 - 500.000
}

public enum ContentType
{
    Video = 1,
    Live = 2,
    Stitch = 3,
    Duet = 4,
}

public enum ProductDelivery
{
    Physical = 1,
    Digital = 2,
    None = 3,
}

public enum ApprovalMode
{
    Auto = 1,
    Manual = 2,
}

public enum CampaignStatus
{
    Draft = 1,
    PendingPayment = 2,
    Active = 3,
    ApplicationsClosed = 4,
    InProgress = 5,
    Completed = 6,
    Cancelled = 7,
}

public enum ApplicationStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Withdrawn = 4,
}

public enum SubmissionStatus
{
    Submitted = 1,
    RevisionRequested = 2,
    Approved = 3,
    Published = 4,
}

public enum PaymentType
{
    Base = 1,
    PerformanceBonus = 2,
    ViralBonus = 3,
    Refund = 4,
}

public enum PaymentStatus
{
    Pending = 1,
    Approved = 2,
    Transferred = 3,
    Failed = 4,
}

public enum BrandPaymentStatus
{
    Pending = 1,
    Received = 2,
    Partial = 3,
    Failed = 4,
}

public enum ReviewerRole
{
    Influencer = 1,
    Brand = 2,
}
