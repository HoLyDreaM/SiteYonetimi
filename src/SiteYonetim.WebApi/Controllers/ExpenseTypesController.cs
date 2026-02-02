using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Cookies,Bearer")]
public class ExpenseTypesController : ControllerBase
{
    private readonly IExpenseTypeService _expenseTypeService;

    public ExpenseTypesController(IExpenseTypeService expenseTypeService) => _expenseTypeService = expenseTypeService;

    [HttpGet]
    [HttpGet("site/{siteId:guid}")]
    public async Task<ActionResult<IReadOnlyList<object>>> GetBySite([FromRoute] Guid? siteId, [FromQuery(Name = "siteId")] Guid? siteIdQuery, CancellationToken ct)
    {
        var id = siteId ?? siteIdQuery;
        // Tarayıcıdan geliyorsa (HTML isteniyorsa) Create formuna yönlendir
        if (Request.Headers.Accept.Any(a => a?.Contains("text/html") == true))
            return Redirect(id.HasValue ? $"/App/ExpenseTypes/Create?siteId={id.Value}" : "/App/ExpenseTypes");
        if (!id.HasValue) return BadRequest(new { error = "siteId gerekli. Örn: /App/ExpenseTypes?siteId=xxx" });
        var list = await _expenseTypeService.GetBySiteIdAsync(id.Value, ct);
        return Ok(list.Select(x => new { x.Id, x.Name, x.ShareType, x.IsRecurring }).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<object>> GetById(Guid id, CancellationToken ct)
    {
        var et = await _expenseTypeService.GetByIdAsync(id, ct);
        if (et == null) return NotFound();
        return Ok(new { et.Id, et.SiteId, et.Name, et.Description, ShareType = (int)et.ShareType, et.IsRecurring, et.RecurringDayOfMonth });
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] CreateExpenseTypeRequest request, CancellationToken ct)
    {
        var et = new ExpenseType
        {
            SiteId = request.SiteId,
            Name = request.Name,
            Description = request.Description,
            ShareType = (ExpenseShareType)request.ShareType,
            IsRecurring = request.IsRecurring,
            RecurringDayOfMonth = request.RecurringDayOfMonth
        };
        et = await _expenseTypeService.CreateAsync(et, ct);
        return CreatedAtAction(nameof(GetById), new { id = et.Id }, new { et.Id, et.SiteId, et.Name });
    }
}

public class CreateExpenseTypeRequest
{
    public Guid SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ShareType { get; set; }
    public bool IsRecurring { get; set; }
    public int? RecurringDayOfMonth { get; set; }
}
