namespace CandleShop.Services.Catalog.API.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => new RedirectResult("~/swagger");
}
