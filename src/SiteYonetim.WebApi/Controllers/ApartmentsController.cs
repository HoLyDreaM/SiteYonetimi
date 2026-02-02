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
public class ApartmentsController : ControllerBase
{
    private readonly IApartmentService _apartmentService;

    public ApartmentsController(IApartmentService apartmentService) => _apartmentService = apartmentService;

    [HttpGet("site/{siteId:guid}")]
    public async Task<ActionResult<IReadOnlyList<ApartmentDto>>> GetBySite(Guid siteId, CancellationToken ct)
    {
        var list = await _apartmentService.GetBySiteIdAsync(siteId, ct);
        return Ok(list.Select(Map).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApartmentDto>> GetById(Guid id, CancellationToken ct)
    {
        var apt = await _apartmentService.GetByIdAsync(id, ct);
        if (apt == null) return NotFound();
        return Ok(Map(apt));
    }

    [HttpPost]
    public async Task<ActionResult<ApartmentDto>> Create([FromBody] CreateApartmentRequest request, CancellationToken ct)
    {
        var apartment = new Apartment
        {
            SiteId = request.SiteId,
            BuildingId = request.BuildingId,
            BlockOrBuildingName = request.BlockOrBuildingName,
            ApartmentNumber = request.ApartmentNumber,
            Floor = request.Floor,
            ShareRate = request.ShareRate,
            OwnerName = request.OwnerName,
            OwnerPhone = request.OwnerPhone,
            OwnerEmail = request.OwnerEmail
        };
        apartment = await _apartmentService.CreateAsync(apartment, ct);
        return CreatedAtAction(nameof(GetById), new { id = apartment.Id }, Map(apartment));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApartmentDto>> Update(Guid id, [FromBody] CreateApartmentRequest request, CancellationToken ct)
    {
        var apt = await _apartmentService.GetByIdAsync(id, ct);
        if (apt == null) return NotFound();
        apt.BuildingId = request.BuildingId;
        apt.BlockOrBuildingName = request.BlockOrBuildingName;
        apt.ApartmentNumber = request.ApartmentNumber;
        apt.Floor = request.Floor;
        apt.ShareRate = request.ShareRate;
        apt.OwnerName = request.OwnerName;
        apt.OwnerPhone = request.OwnerPhone;
        apt.OwnerEmail = request.OwnerEmail;
        apt = await _apartmentService.UpdateAsync(apt, ct);
        return Ok(Map(apt));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _apartmentService.DeleteAsync(id, ct);
        return NoContent();
    }

    private static ApartmentDto Map(Apartment a) => new()
    {
        Id = a.Id,
        SiteId = a.SiteId,
        BuildingId = a.BuildingId,
        BlockOrBuildingName = a.BlockOrBuildingName,
        ApartmentNumber = a.ApartmentNumber,
        Floor = a.Floor,
        ShareRate = a.ShareRate,
        OwnerName = a.OwnerName,
        OwnerPhone = a.OwnerPhone,
        OwnerEmail = a.OwnerEmail
    };
}
