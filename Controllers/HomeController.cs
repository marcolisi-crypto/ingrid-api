using Microsoft.AspNetCore.Mvc;
namespace AIReception.Mvc.Controllers;
public class HomeController : Controller
{
    public IActionResult Index() => Content("AI Reception C# Running");
}