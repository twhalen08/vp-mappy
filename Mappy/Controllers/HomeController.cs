using Microsoft.AspNetCore.Mvc;

namespace VirtualParadiseApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
