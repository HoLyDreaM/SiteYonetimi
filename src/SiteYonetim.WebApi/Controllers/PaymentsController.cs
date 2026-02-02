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
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService) => _paymentService = paymentService;

    [HttpGet("site/{siteId:guid}")]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> GetBySite(Guid siteId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? apartmentId, CancellationToken ct)
    {
        var list = await _paymentService.GetBySiteIdAsync(siteId, from, to, apartmentId, ct);
        return Ok(list.Select(Map).ToList());
    }

    [HttpGet("apartment/{apartmentId:guid}")]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> GetByApartment(Guid apartmentId, CancellationToken ct)
    {
        var list = await _paymentService.GetByApartmentIdAsync(apartmentId, ct);
        return Ok(list.Select(Map).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentDto>> GetById(Guid id, CancellationToken ct)
    {
        var p = await _paymentService.GetByIdAsync(id, ct);
        if (p == null) return NotFound();
        return Ok(Map(p));
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Create([FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        var payment = new Payment
        {
            SiteId = request.SiteId,
            ApartmentId = request.ApartmentId,
            ExpenseShareId = request.ExpenseShareId,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate,
            Method = (PaymentMethod)request.Method,
            ReferenceNumber = request.ReferenceNumber,
            Description = request.Description,
            BankAccountId = request.BankAccountId
        };
        payment = await _paymentService.CreateAsync(payment, ct);
        if (request.CreateReceipt)
            await _paymentService.CreateReceiptAsync(payment.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, Map(payment));
    }

    [HttpPost("{id:guid}/receipt")]
    public async Task<ActionResult<object>> CreateReceipt(Guid id, CancellationToken ct)
    {
        var receipt = await _paymentService.CreateReceiptAsync(id, ct);
        if (receipt == null) return NotFound();
        return Ok(new { receipt.Id, receipt.ReceiptNumber, receipt.Amount, receipt.ReceiptDate });
    }

    private static PaymentDto Map(Payment p) => new()
    {
        Id = p.Id,
        SiteId = p.SiteId,
        ApartmentId = p.ApartmentId,
        ApartmentNumber = p.Apartment?.ApartmentNumber ?? "",
        ExpenseShareId = p.ExpenseShareId,
        Amount = p.Amount,
        PaymentDate = p.PaymentDate,
        Method = (int)p.Method,
        ReferenceNumber = p.ReferenceNumber,
        Description = p.Description
    };
}
