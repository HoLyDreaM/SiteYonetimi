using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class ExpensesController : Controller
{
    private readonly IExpenseService _expenseService;
    private readonly IExpenseTypeService _expenseTypeService;
    private readonly ISiteService _siteService;
    private readonly IInvoiceExpenseAutoDeductionService _autoDeductionService;

    public ExpensesController(IExpenseService expenseService, IExpenseTypeService expenseTypeService, ISiteService siteService, IInvoiceExpenseAutoDeductionService autoDeductionService)
    {
        _expenseService = expenseService;
        _expenseTypeService = expenseTypeService;
        _siteService = siteService;
        _autoDeductionService = autoDeductionService;
    }

    public async Task<IActionResult> Index(Guid? siteId, CancellationToken ct)
    {
        if (!siteId.HasValue)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account", new { area = "" });
            var sites = await _siteService.GetUserSitesAsync(userId, ct);
            ViewBag.Sites = sites;
            ViewBag.PageTitle = "Giderler - Site Seçin";
            return View("SelectSite");
        }
        var list = await _expenseService.GetBySiteIdAsync(siteId.Value, null, null, ct);
        var site = await _siteService.GetByIdAsync(siteId.Value, ct);
        ViewBag.SiteId = siteId;
        ViewBag.SiteName = site?.Name ?? "";
        return View(list);
    }

    public async Task<IActionResult> Edit(Guid id, Guid? siteId, CancellationToken ct)
    {
        var expense = await _expenseService.GetByIdAsync(id, ct);
        if (expense == null) return NotFound();
        var types = await _expenseTypeService.GetBySiteIdAsync(expense.SiteId, ct);
        ViewBag.SiteId = expense.SiteId;
        ViewBag.ExpenseTypes = types;
        return View(expense);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,SiteId,ExpenseTypeId,Description,Amount,InvoiceDate,InvoiceNumber,Notes,Status")] Expense model, CancellationToken ct)
    {
        ModelState.Remove("Site");
        ModelState.Remove("ExpenseType");
        if (id != model.Id) return BadRequest();
        if (model.ExpenseTypeId == Guid.Empty)
            ModelState.AddModelError("ExpenseTypeId", "Gider türü seçin.");
        if (string.IsNullOrWhiteSpace(model.Description))
            ModelState.AddModelError("Description", "Açıklama gerekli.");
        if (model.Amount <= 0)
            ModelState.AddModelError("Amount", "Tutar 0'dan büyük olmalı.");
        if (!model.InvoiceDate.HasValue)
            ModelState.AddModelError("InvoiceDate", "Fatura tarihi gerekli.");
        if (!ModelState.IsValid)
        {
            ViewBag.SiteId = model.SiteId;
            ViewBag.ExpenseTypes = await _expenseTypeService.GetBySiteIdAsync(model.SiteId, ct);
            return View(model);
        }
        try
        {
            model.ExpenseDate = model.InvoiceDate!.Value;
            model.DueDate = model.InvoiceDate.Value;
            await _expenseService.UpdateAsync(model, ct);
            await _autoDeductionService.ProcessDueExpensesAsync(ct);
            return RedirectToAction(nameof(Index), new { area = "App", siteId = model.SiteId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Güncelleme sırasında hata: {ex.Message}");
            ViewBag.SiteId = model.SiteId;
            ViewBag.ExpenseTypes = await _expenseTypeService.GetBySiteIdAsync(model.SiteId, ct);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid siteId, CancellationToken ct)
    {
        await _expenseService.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId });
    }

    public async Task<IActionResult> Create(Guid siteId, CancellationToken ct)
    {
        if (siteId == Guid.Empty)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account", new { area = "" });
            var sites = await _siteService.GetUserSitesAsync(userId, ct);
            ViewBag.Sites = sites;
            ViewBag.PageTitle = "Giderler - Site Seçin";
            return View("SelectSite");
        }
        var types = await _expenseTypeService.GetBySiteIdAsync(siteId, ct);
        if (types.Count == 0)
            return RedirectToAction("Create", "ExpenseTypes", new { area = "App", siteId });
        ViewBag.SiteId = siteId;
        ViewBag.ExpenseTypes = types;
        return View(new Expense { SiteId = siteId, ExpenseDate = DateTime.Today, InvoiceDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("SiteId,ExpenseTypeId,Description,Amount,InvoiceDate,InvoiceNumber,Notes")] Expense model, CancellationToken ct)
    {
        ModelState.Remove("Site");
        ModelState.Remove("ExpenseType");
        if (model.SiteId == Guid.Empty)
            ModelState.AddModelError("", "Site seçimi gerekli.");
        if (model.ExpenseTypeId == Guid.Empty)
            ModelState.AddModelError("ExpenseTypeId", "Gider türü seçin.");
        if (string.IsNullOrWhiteSpace(model.Description))
            ModelState.AddModelError("Description", "Açıklama gerekli.");
        if (model.Amount <= 0)
            ModelState.AddModelError("Amount", "Tutar 0'dan büyük olmalı.");
        if (!model.InvoiceDate.HasValue)
            ModelState.AddModelError("InvoiceDate", "Fatura tarihi gerekli.");
        if (!ModelState.IsValid)
        {
            ViewBag.SiteId = model.SiteId;
            ViewBag.ExpenseTypes = model.SiteId != Guid.Empty ? await _expenseTypeService.GetBySiteIdAsync(model.SiteId, ct) : new List<ExpenseType>();
            return View(model);
        }
        try
        {
            model.ExpenseDate = model.InvoiceDate!.Value;
            model.DueDate = model.InvoiceDate.Value;
            model.Status = ExpenseStatus.Draft;
            model.IsDeleted = false;
            await _expenseService.CreateAsync(model, ct);
            await _autoDeductionService.ProcessDueExpensesAsync(ct);
            return RedirectToAction(nameof(Index), new { area = "App", siteId = model.SiteId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Kayıt sırasında hata: {ex.Message}");
            ViewBag.SiteId = model.SiteId;
            ViewBag.ExpenseTypes = await _expenseTypeService.GetBySiteIdAsync(model.SiteId, ct);
            return View(model);
        }
    }

}
