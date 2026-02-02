using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.WebApi.Models;

namespace SiteYonetim.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ExpenseSharesController : ControllerBase
{
    private readonly IExpenseShareService _expenseShareService;

    public ExpenseSharesController(IExpenseShareService expenseShareService) => _expenseShareService = expenseShareService;

    [HttpGet("site/{siteId:guid}")]
    public async Task<ActionResult<IReadOnlyList<ExpenseShareDto>>> GetBySite(Guid siteId, [FromQuery] Guid? apartmentId, [FromQuery] int? status, CancellationToken ct)
    {
        var list = await _expenseShareService.GetBySiteIdAsync(siteId, apartmentId, status, ct);
        return Ok(list.Select(Map).ToList());
    }

    [HttpGet("apartment/{apartmentId:guid}")]
    public async Task<ActionResult<IReadOnlyList<ExpenseShareDto>>> GetByApartment(Guid apartmentId, [FromQuery] bool onlyUnpaid = false, CancellationToken ct = default)
    {
        var list = await _expenseShareService.GetByApartmentIdAsync(apartmentId, onlyUnpaid, ct);
        return Ok(list.Select(Map).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExpenseShareDto>> GetById(Guid id, CancellationToken ct)
    {
        var share = await _expenseShareService.GetByIdAsync(id, ct);
        if (share == null) return NotFound();
        return Ok(Map(share));
    }

    [HttpPost("site/{siteId:guid}/apply-late-fees")]
    public async Task<ActionResult> ApplyLateFees(Guid siteId, CancellationToken ct)
    {
        await _expenseShareService.ApplyLateFeesAsync(siteId, ct);
        return Ok(new { message = "Gecikme zammı uygulandı." });
    }

    private static ExpenseShareDto Map(Domain.Entities.ExpenseShare s) => new()
    {
        Id = s.Id,
        ExpenseId = s.ExpenseId,
        ApartmentId = s.ApartmentId,
        ApartmentNumber = s.Apartment?.ApartmentNumber ?? "",
        BlockOrBuildingName = s.Apartment?.BlockOrBuildingName,
        ExpenseTypeName = s.Expense?.ExpenseType?.Name ?? "",
        Amount = s.Amount,
        LateFeeAmount = s.LateFeeAmount,
        TotalAmount = s.TotalAmount,
        PaidAmount = s.PaidAmount,
        Balance = s.Balance,
        Status = (int)s.Status,
        DueDate = s.DueDate
    };
}
