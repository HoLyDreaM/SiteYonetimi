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
public class MetersController : ControllerBase
{
    private readonly IMeterService _meterService;

    public MetersController(IMeterService meterService) => _meterService = meterService;

    [HttpGet("site/{siteId:guid}")]
    public async Task<ActionResult<IReadOnlyList<MeterDto>>> GetBySite(Guid siteId, [FromQuery] Guid? apartmentId, CancellationToken ct)
    {
        var list = await _meterService.GetBySiteIdAsync(siteId, apartmentId, ct);
        return Ok(list.Select(Map).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MeterDto>> GetById(Guid id, CancellationToken ct)
    {
        var m = await _meterService.GetByIdAsync(id, ct);
        if (m == null) return NotFound();
        return Ok(Map(m));
    }

    [HttpPost]
    public async Task<ActionResult<MeterDto>> Create([FromBody] CreateMeterRequest request, CancellationToken ct)
    {
        var meter = new Meter
        {
            SiteId = request.SiteId,
            ApartmentId = request.ApartmentId,
            Name = request.Name,
            Type = request.Type ?? string.Empty,
            SerialNumber = request.SerialNumber,
            Unit = request.Unit,
            Multiplier = request.Multiplier ?? 1
        };
        meter = await _meterService.CreateAsync(meter, ct);
        return CreatedAtAction(nameof(GetById), new { id = meter.Id }, Map(meter));
    }

    [HttpGet("{id:guid}/readings")]
    public async Task<ActionResult<IReadOnlyList<MeterReadingDto>>> GetReadings(Guid id, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var list = await _meterService.GetReadingsAsync(id, from, to, ct);
        return Ok(list.Select(Map).ToList());
    }

    [HttpPost("readings")]
    public async Task<ActionResult<MeterReadingDto>> AddReading([FromBody] CreateMeterReadingRequest request, CancellationToken ct)
    {
        var reading = new MeterReading
        {
            MeterId = request.MeterId,
            ReadingValue = request.ReadingValue,
            ReadingDate = request.ReadingDate,
            Notes = request.Notes,
            IsEstimated = request.IsEstimated
        };
        reading = await _meterService.AddReadingAsync(reading, ct);
        return Ok(Map(reading));
    }

    private static MeterDto Map(Meter m) => new()
    {
        Id = m.Id,
        SiteId = m.SiteId,
        ApartmentId = m.ApartmentId,
        Name = m.Name,
        Type = m.Type ?? string.Empty,
        SerialNumber = m.SerialNumber,
        Unit = m.Unit,
        Multiplier = m.Multiplier
    };

    private static MeterReadingDto Map(MeterReading r) => new()
    {
        Id = r.Id,
        MeterId = r.MeterId,
        ReadingValue = r.ReadingValue,
        ReadingDate = r.ReadingDate,
        PreviousReadingValue = r.PreviousReadingValue,
        Consumption = r.Consumption,
        IsEstimated = r.IsEstimated
    };
}
