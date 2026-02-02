namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Sayaç (Su, elektrik, doğalgaz vb.)
/// </summary>
public class Meter : BaseEntity
{
    public Guid SiteId { get; set; }
    public Guid? ApartmentId { get; set; } // Daireye özel sayaç (null ise ortak)
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Unit { get; set; } // m³, kWh vb.
    public decimal? Multiplier { get; set; } = 1; // Çarpan

    public Site Site { get; set; } = null!;
    public Apartment? Apartment { get; set; }
    public ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();
}
