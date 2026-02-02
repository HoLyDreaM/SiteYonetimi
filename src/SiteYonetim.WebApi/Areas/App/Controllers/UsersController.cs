using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class UsersController : Controller
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    public async Task<IActionResult> Index(bool? pendingOnly, CancellationToken ct = default)
    {
        var list = await _userService.GetAllAsync(pendingOnly, ct);
        ViewBag.PendingOnly = pendingOnly ?? false;
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct = default)
    {
        await _userService.ApproveAsync(id, ct);
        return RedirectToAction(nameof(Index), new { area = "App", pendingOnly = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await _userService.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index), new { area = "App" });
    }
}
