using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class BankAccountsController : Controller
{
    private readonly IBankAccountService _bankService;
    private readonly ISiteService _siteService;
    private readonly IInvoiceExpenseAutoDeductionService _autoDeductionService;

    public BankAccountsController(IBankAccountService bankService, ISiteService siteService, IInvoiceExpenseAutoDeductionService autoDeductionService)
    {
        _bankService = bankService;
        _siteService = siteService;
        _autoDeductionService = autoDeductionService;
    }

    public async Task<IActionResult> Index(Guid? siteId, CancellationToken ct = default)
    {
        if (!siteId.HasValue)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account", new { area = "" });
            ViewBag.Sites = await _siteService.GetUserSitesAsync(userId, ct);
            ViewBag.PageTitle = "Banka Hesapları - Site Seçin";
            return View("SelectSite");
        }
        await _autoDeductionService.ProcessDueExpensesAsync(ct);
        await _bankService.SyncBalancesForSiteAsync(siteId.Value, ct);
        var list = await _bankService.GetBySiteIdAsync(siteId.Value, ct);
        var model = new List<(BankAccount Account, decimal Balance)>();
        foreach (var b in list)
            model.Add((b, await _bankService.GetEffectiveBalanceAsync(b.Id, ct)));
        ViewBag.SiteId = siteId;
        return View(model);
    }

    public async Task<IActionResult> Create(Guid siteId, CancellationToken ct = default)
    {
        var site = await _siteService.GetByIdAsync(siteId, ct);
        if (site == null) return NotFound();
        ViewBag.SiteId = siteId;
        ViewBag.SiteName = site.Name;
        return View(new BankAccount { SiteId = siteId, Currency = "TRY", IsDeleted = false });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BankAccount model, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(model.BankName) || string.IsNullOrWhiteSpace(model.AccountNumber))
        {
            ModelState.AddModelError("", "Banka adı ve hesap numarası gerekli.");
            ViewBag.SiteId = model.SiteId;
            var site = await _siteService.GetByIdAsync(model.SiteId, ct);
            ViewBag.SiteName = site?.Name ?? "";
            return View(model);
        }
        await _bankService.CreateAsync(model, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId = model.SiteId });
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
    {
        var acc = await _bankService.GetByIdAsync(id, ct);
        if (acc == null) return NotFound();
        ViewBag.EffectiveBalance = await _bankService.GetEffectiveBalanceAsync(id, ct);
        return View(acc);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, BankAccount model, CancellationToken ct = default)
    {
        if (id != model.Id) return BadRequest();
        await _bankService.UpdateAsync(model, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId = model.SiteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid siteId, CancellationToken ct = default)
    {
        await _bankService.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reconcile(Guid id, Guid siteId, decimal realBalance, CancellationToken ct = default)
    {
        await _bankService.ReconcileBalanceAsync(id, realBalance, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId });
    }
}
