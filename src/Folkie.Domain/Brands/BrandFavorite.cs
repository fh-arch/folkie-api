using Folkie.Domain.Common;

namespace Folkie.Domain.Brands;

/// <summary>
/// Markaların kaydettiği creator listesi. Bir creator birden fazla marka tarafından
/// favorilenebilir; aynı marka-creator çifti tekrar eklenemez (unique index).
/// </summary>
public class BrandFavorite : Entity
{
    public Guid BrandProfileId { get; private set; }
    public Guid InfluencerProfileId { get; private set; }
    public string? Note { get; private set; }

    private BrandFavorite() { }

    public static BrandFavorite Create(
        Guid brandProfileId,
        Guid influencerProfileId,
        string? note = null)
    {
        return new BrandFavorite
        {
            BrandProfileId = brandProfileId,
            InfluencerProfileId = influencerProfileId,
            Note = note,
        };
    }

    public void UpdateNote(string? note)
    {
        Note = note;
        Touch();
    }
}
