namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Blok (Site birden fazla bloktan oluşuyorsa)
/// </summary>
public class Building : BaseEntity
{
    public Guid SiteId { get; set; }
    public string Name { get; set; } = string.Empty; // A Blok, B Blok vb.
    public int? FloorCount { get; set; }

    public Site Site { get; set; } = null!;
    public ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();
}
