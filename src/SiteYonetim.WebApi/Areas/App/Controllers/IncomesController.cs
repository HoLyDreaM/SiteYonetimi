using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class IncomesController : Controller
{
    private readonly IIncomeService _incomeService;
    private readonly ISiteService _siteService;
    private readonly IPaymentService _paymentService;
    private readonly IBankAccountService _bankService;
    private readonly IApartmentService _apartmentService;

    public IncomesController(IIncomeService incomeService, ISiteService siteService, IPaymentService paymentService, IBankAccountService bankService, IApartmentService apartmentService)
    {
        _incomeService = incomeService;
        _siteService = siteService;
        _paymentService = paymentService;
        _bankService = bankService;
        _apartmentService = apartmentService;
    }

    public async Task<IActionResult> Index(Guid? siteId, int? year, int? month, CancellationToken ct = default)
    {
        if (!siteId.HasValue)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account", new { area = "" });
            ViewBag.Sites = await _siteService.GetUserSitesAsync(userId, ct);
            ViewBag.PageTitle = "Gelirler - Site Seçin";
            ViewBag.Year = DateTime.Today.Year;
            ViewBag.Month = DateTime.Today.Month;
            return View("SelectSite");
        }
        var y = year ?? DateTime.Today.Year;
        var m = month ?? DateTime.Today.Month;
        var list = await _incomeService.GetBySiteIdAsync(siteId.Value, y, m, ct);
        var paidAmounts = new Dictionary<Guid, decimal>();
        decimal totalCollected = 0, totalPending = 0;
        foreach (var inc in list)
        {
            var paid = await _incomeService.GetPaidAmountAsync(inc.Id, ct);
            paidAmounts[inc.Id] = paid;
            totalCollected += paid;
            totalPending += inc.Amount - paid;
        }
        ViewBag.SiteId = siteId;
        ViewBag.Year = y;
        ViewBag.Month = m;
        ViewBag.TotalCollected = totalCollected;
        ViewBag.TotalPending = totalPending;
        ViewBag.PaidAmounts = paidAmounts;
        ViewBag.ApartmentCount = (await _apartmentService.GetBySiteIdAsync(siteId.Value, ct)).Count;
        return View(list);
    }

    public async Task<IActionResult> CreateExtraCollection(Guid siteId, int year, int month, CancellationToken ct = default)
    {
        var site = await _siteService.GetByIdAsync(siteId, ct);
        if (site == null) return NotFound();
        var apartments = await _apartmentService.GetBySiteIdAsync(siteId, ct);
        ViewBag.SiteId = siteId;
        ViewBag.Year = year;
        ViewBag.Month = month;
        ViewBag.SiteName = site.Name;
        ViewBag.ApartmentCount = apartments.Count;
        return View(new CreateExtraCollectionModel { SiteId = siteId, Year = year, Month = month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateExtraCollection(CreateExtraCollectionModel model, CancellationToken ct = default)
    {
        if (model.TotalAmount <= 0)
        {
            ModelState.AddModelError("TotalAmount", "Toplam tutar 0'dan büyük olmalı.");
        }
        if (string.IsNullOrWhiteSpace(model.Description))
        {
            ModelState.AddModelError("Description", "Açıklama gerekli (örn: 2024 Yıllık Toplantı - Fon).");
        }
        if (!ModelState.IsValid)
        {
            var site = await _siteService.GetByIdAsync(model.SiteId, ct);
            ViewBag.SiteId = model.SiteId;
            ViewBag.Year = model.Year;
            ViewBag.Month = model.Month;
            ViewBag.SiteName = site?.Name ?? "";
            ViewBag.ApartmentCount = (await _apartmentService.GetBySiteIdAsync(model.SiteId, ct)).Count;
            return View(model);
        }
        await _incomeService.CreateExtraCollectionAsync(model.SiteId, model.Year, model.Month, model.TotalAmount, model.Description!.Trim(), ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId = model.SiteId, year = model.Year, month = model.Month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMonth(Guid siteId, int year, int month, CancellationToken ct = default)
    {
        await _incomeService.EnsureMonthlyIncomesAsync(year, month, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId, year, month });
    }

    public async Task<IActionResult> Collect(Guid id, CancellationToken ct = default)
    {
        var income = await _incomeService.GetByIdAsync(id, ct);
        if (income == null) return NotFound();
        var paidAmount = await _incomeService.GetPaidAmountAsync(id, ct);
        var remainingAmount = income.Amount - paidAmount;
        var banks = await _bankService.GetBySiteIdAsync(income.SiteId, ct);
        ViewBag.Income = income;
        ViewBag.PaidAmount = paidAmount;
        ViewBag.RemainingAmount = remainingAmount;
        ViewBag.BankAccounts = banks;
        return View(new CollectIncomeModel { IncomeId = id, Amount = remainingAmount, PaymentDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Collect(CollectIncomeModel model, CancellationToken ct = default)
    {
        var income = await _incomeService.GetByIdAsync(model.IncomeId, ct);
        if (income == null) return NotFound();
        var paidAmount = await _incomeService.GetPaidAmountAsync(model.IncomeId, ct);
        var remainingAmount = income.Amount - paidAmount;
        if (model.Amount <= 0)
        {
            ModelState.AddModelError("Amount", "Tutar 0'dan büyük olmalı.");
        }
        if (!ModelState.IsValid)
        {
            ViewBag.Income = income;
            ViewBag.PaidAmount = paidAmount;
            ViewBag.RemainingAmount = remainingAmount;
            ViewBag.BankAccounts = await _bankService.GetBySiteIdAsync(income.SiteId, ct);
            return View(model);
        }
        var desc = income.Type == IncomeType.ExtraCollection
            ? (income.Description ?? $"Özel toplama - {income.Year}/{income.Month:D2}")
            : $"Aidat tahsilatı - {income.Year}/{income.Month:D2}";
        var payment = new Payment
        {
            SiteId = income.SiteId,
            ApartmentId = income.ApartmentId,
            IncomeId = income.Id,
            Amount = model.Amount,
            PaymentDate = model.PaymentDate,
            Method = PaymentMethod.BankTransfer,
            Description = desc,
            BankAccountId = model.BankAccountId,
            IsDeleted = false
        };
        await _paymentService.CreateAsync(payment, ct);
        await _incomeService.MarkAsPaidAsync(income.Id, payment.Id, model.Amount, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId = income.SiteId, year = income.Year, month = income.Month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid siteId, int year, int month, CancellationToken ct = default)
    {
        var deleted = await _incomeService.DeleteAsync(id, ct);
        if (!deleted)
            TempData["Error"] = "Bu gelirde tahsilat yapılmış olduğu için silinemez.";
        return RedirectToAction(nameof(Index), new { area = "App", siteId, year, month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBulk(Guid siteId, int year, int month, [FromForm] List<Guid>? incomeIds, CancellationToken ct = default)
    {
        var ids = incomeIds ?? new List<Guid>();
        var deleted = await _incomeService.DeleteBulkAsync(ids, ct);
        if (deleted > 0)
            TempData["Message"] = $"{deleted} gelir kaydı silindi.";
        if (ids.Count > 0 && deleted < ids.Count)
            TempData["Error"] = $"Bazı kayıtlarda tahsilat olduğu için silinemedi. {deleted}/{ids.Count} kayıt silindi.";
        return RedirectToAction(nameof(Index), new { area = "App", siteId, year, month });
    }
}

public class CollectIncomeModel
{
    public Guid IncomeId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public Guid? BankAccountId { get; set; }
}

public class CreateExtraCollectionModel
{
    public Guid SiteId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Description { get; set; }
}
