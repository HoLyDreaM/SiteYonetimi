using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.WebApi.Models;

namespace SiteYonetim.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SitesController : ControllerBase
{
    private readonly ISiteService _siteService;

    public SitesController(ISiteService siteService) => _siteService = siteService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SiteDto>>> GetMySites(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();
        var list = await _siteService.GetUserSitesAsync(userId, ct);
        return Ok(list.Select(Map).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SiteDto>> GetById(Guid id, CancellationToken ct)
    {
        var site = await _siteService.GetByIdAsync(id, ct);
        if (site == null) return NotFound();
        return Ok(Map(site));
    }

    [HttpPost]
    public async Task<ActionResult<SiteDto>> Create([FromBody] CreateSiteRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = Guid.TryParse(userIdClaim, out var u) ? u : null;
        var site = new Site
        {
            Name = request.Name,
            Address = request.Address,
            City = request.City,
            District = request.District,
            TaxOffice = request.TaxOffice,
            TaxNumber = request.TaxNumber,
            LateFeeRate = request.LateFeeRate,
            LateFeeDay = request.LateFeeDay,
            HasMultipleBlocks = request.HasMultipleBlocks
        };
        site = await _siteService.CreateAsync(site, userId, ct);
        return CreatedAtAction(nameof(GetById), new { id = site.Id }, Map(site));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SiteDto>> Update(Guid id, [FromBody] CreateSiteRequest request, CancellationToken ct)
    {
        var site = await _siteService.GetByIdAsync(id, ct);
        if (site == null) return NotFound();
        site.Name = request.Name;
        site.Address = request.Address;
        site.City = request.City;
        site.District = request.District;
        site.TaxOffice = request.TaxOffice;
        site.TaxNumber = request.TaxNumber;
        site.LateFeeRate = request.LateFeeRate;
        site.LateFeeDay = request.LateFeeDay;
        site.HasMultipleBlocks = request.HasMultipleBlocks;
        site = await _siteService.UpdateAsync(site, ct);
        return Ok(Map(site));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _siteService.DeleteAsync(id, ct);
        return NoContent();
    }

    private static SiteDto Map(Site s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Address = s.Address,
        City = s.City,
        District = s.District,
        TaxOffice = s.TaxOffice,
        TaxNumber = s.TaxNumber,
        LateFeeRate = s.LateFeeRate,
        LateFeeDay = s.LateFeeDay,
        HasMultipleBlocks = s.HasMultipleBlocks
    };
}
