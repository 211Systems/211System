using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace _211system.Areas.Medic.Controllers
{
    [Area("Medic")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}