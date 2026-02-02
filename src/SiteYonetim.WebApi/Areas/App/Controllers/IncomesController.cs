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

    public IncomesController(IIncomeService incomeService, ISiteService siteService, IPaymentService paymentService, IBankAccountService bankService)
    {
        _incomeService = incomeService;
        _siteService = siteService;
        _paymentService = paymentService;
        _bankService = bankService;
    }

    public async Task<IActionResult> Index(Guid? siteId, int? year, int? month, CancellationToken ct = default)
    {
        if (!siteId.HasValue)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account", new { area = "" });
            ViewBag.Sites = await _siteService.GetUserSitesAsync(userId, ct);
            ViewBag.PageTitle = "Gelirler (Aidat) - Site Seçin";
            return View("SelectSite");
        }
        var y = year ?? DateTime.Today.Year;
        var m = month ?? DateTime.Today.Month;
        var list = await _incomeService.GetBySiteIdAsync(siteId.Value, y, m, ct);
        ViewBag.SiteId = siteId;
        ViewBag.Year = y;
        ViewBag.Month = m;
        ViewBag.Total = list.Sum(x => x.Amount);
        return View(list);
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
        var banks = await _bankService.GetBySiteIdAsync(income.SiteId, ct);
        ViewBag.Income = income;
        ViewBag.BankAccounts = banks;
        return View(new CollectIncomeModel { IncomeId = id, Amount = income.Amount, PaymentDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Collect(CollectIncomeModel model, CancellationToken ct = default)
    {
        var income = await _incomeService.GetByIdAsync(model.IncomeId, ct);
        if (income == null) return NotFound();
        if (model.Amount <= 0) { ModelState.AddModelError("Amount", "Tutar 0'dan büyük olmalı."); }
        if (!ModelState.IsValid)
        {
            ViewBag.Income = income;
            ViewBag.BankAccounts = await _bankService.GetBySiteIdAsync(income.SiteId, ct);
            return View(model);
        }
        var payment = new Payment
        {
            SiteId = income.SiteId,
            ApartmentId = income.ApartmentId,
            IncomeId = income.Id,
            Amount = model.Amount,
            PaymentDate = model.PaymentDate,
            Method = PaymentMethod.BankTransfer,
            Description = $"Aidat tahsilatı - {income.Year}/{income.Month:D2}",
            BankAccountId = model.BankAccountId,
            IsDeleted = false
        };
        await _paymentService.CreateAsync(payment, ct);
        await _incomeService.MarkAsPaidAsync(income.Id, payment.Id, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId = income.SiteId, year = income.Year, month = income.Month });
    }
}

public class CollectIncomeModel
{
    public Guid IncomeId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public Guid? BankAccountId { get; set; }
}
