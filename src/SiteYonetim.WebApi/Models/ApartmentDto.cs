namespace SiteYonetim.WebApi.Models;

public class ApartmentDto
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public Guid? BuildingId { get; set; }
    public string BlockOrBuildingName { get; set; } = string.Empty;
    public string ApartmentNumber { get; set; } = string.Empty;
    public int? Floor { get; set; }
    public decimal ShareRate { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
    public string? OwnerEmail { get; set; }
}

public class CreateApartmentRequest
{
    public Guid SiteId { get; set; }
    public Guid? BuildingId { get; set; }
    public string BlockOrBuildingName { get; set; } = string.Empty;
    public string ApartmentNumber { get; set; } = string.Empty;
    public int? Floor { get; set; }
    public decimal ShareRate { get; set; } = 1;
    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
    public string? OwnerEmail { get; set; }
}
