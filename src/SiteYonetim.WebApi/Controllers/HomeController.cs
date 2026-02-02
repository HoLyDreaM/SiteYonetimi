using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SiteYonetim.WebApi.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/Dashboard");
        return RedirectToAction("Login", "Account");
    }
}
