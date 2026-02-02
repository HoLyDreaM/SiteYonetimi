namespace SiteYonetim.WebApi.Models;

public class SiteDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public decimal? LateFeeRate { get; set; }
    public int? LateFeeDay { get; set; }
    public bool HasMultipleBlocks { get; set; }
}

public class CreateSiteRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public decimal? LateFeeRate { get; set; }
    public int? LateFeeDay { get; set; }
    public bool HasMultipleBlocks { get; set; }
}
