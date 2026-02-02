namespace SiteYonetim.WebApi.Models;

public class MeterDto
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public Guid? ApartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Unit { get; set; }
    public decimal? Multiplier { get; set; }
}

public class MeterReadingDto
{
    public Guid Id { get; set; }
    public Guid MeterId { get; set; }
    public decimal ReadingValue { get; set; }
    public DateTime ReadingDate { get; set; }
    public decimal? PreviousReadingValue { get; set; }
    public decimal? Consumption { get; set; }
    public bool IsEstimated { get; set; }
}

public class CreateMeterRequest
{
    public Guid SiteId { get; set; }
    public Guid? ApartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Unit { get; set; }
    public decimal? Multiplier { get; set; }
}

public class CreateMeterReadingRequest
{
    public Guid MeterId { get; set; }
    public decimal ReadingValue { get; set; }
    public DateTime ReadingDate { get; set; }
    public string? Notes { get; set; }
    public bool IsEstimated { get; set; }
}
