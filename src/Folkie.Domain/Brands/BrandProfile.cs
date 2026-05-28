using Folkie.Domain.Common;

namespace Folkie.Domain.Brands;

public class BrandProfile : Entity
{
    public Guid UserId { get; private set; }
    public string BrandName { get; private set; } = string.Empty;
    public string? TaxId { get; private set; }
    public string? Industry { get; private set; }
    public string? Website { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? ContactName { get; private set; }
    public string? ContactPhone { get; private set; }
    public string? BillingAddress { get; private set; }
    public bool IsVerified { get; private set; }
    public bool IsActive { get; private set; } = true;

    private BrandProfile() { }

    public static BrandProfile Create(Guid userId, string brandName)
    {
        return new BrandProfile
        {
            UserId = userId,
            BrandName = brandName,
        };
    }

    public void UpdateProfile(
        string brandName,
        string? taxId,
        string? industry,
        string? website,
        string? logoUrl,
        string? contactName,
        string? contactPhone,
        string? billingAddress)
    {
        BrandName = brandName;
        TaxId = taxId;
        Industry = industry;
        Website = website;
        LogoUrl = logoUrl;
        ContactName = contactName;
        ContactPhone = contactPhone;
        BillingAddress = billingAddress;
        Touch();
    }

    public void Verify() { IsVerified = true; Touch(); }
    public void Deactivate() { IsActive = false; Touch(); }
    public void Activate() { IsActive = true; Touch(); }
}
