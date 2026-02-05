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
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService) => _expenseService = expenseService;

    [HttpGet("site/{siteId:guid}")]
    public async Task<ActionResult<IReadOnlyList<ExpenseDto>>> GetBySite(Guid siteId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var list = await _expenseService.GetBySiteIdAsync(siteId, from, to, ct);
        return Ok(list.Select(Map).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExpenseDto>> GetById(Guid id, CancellationToken ct)
    {
        var e = await _expenseService.GetByIdAsync(id, includeAttachments: false, ct);
        if (e == null) return NotFound();
        return Ok(Map(e));
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> Create([FromBody] CreateExpenseRequest request, CancellationToken ct)
    {
        var expense = new Expense
        {
            SiteId = request.SiteId,
            ExpenseTypeId = request.ExpenseTypeId,
            Description = request.Description,
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate,
            DueDate = request.DueDate,
            InvoiceNumber = request.InvoiceNumber,
            Notes = request.Notes,
            Status = ExpenseStatus.Draft
        };
        expense = await _expenseService.CreateAsync(expense, ct);
        return CreatedAtAction(nameof(GetById), new { id = expense.Id }, Map(expense));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExpenseDto>> Update(Guid id, [FromBody] CreateExpenseRequest request, CancellationToken ct)
    {
        var e = await _expenseService.GetByIdAsync(id, includeAttachments: false, ct);
        if (e == null) return NotFound();
        e.ExpenseTypeId = request.ExpenseTypeId;
        e.Description = request.Description;
        e.Amount = request.Amount;
        e.ExpenseDate = request.ExpenseDate;
        e.DueDate = request.DueDate;
        e.InvoiceNumber = request.InvoiceNumber;
        e.Notes = request.Notes;
        e = await _expenseService.UpdateAsync(e, ct);
        return Ok(Map(e));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _expenseService.DeleteAsync(id, ct);
        return NoContent();
    }

    private static ExpenseDto Map(Expense e) => new()
    {
        Id = e.Id,
        SiteId = e.SiteId,
        ExpenseTypeId = e.ExpenseTypeId,
        ExpenseTypeName = e.ExpenseType?.Name ?? "",
        Description = e.Description,
        Amount = e.Amount,
        ExpenseDate = e.ExpenseDate,
        DueDate = e.DueDate,
        Status = (int)e.Status,
        InvoiceNumber = e.InvoiceNumber,
        Notes = e.Notes
    };
}
