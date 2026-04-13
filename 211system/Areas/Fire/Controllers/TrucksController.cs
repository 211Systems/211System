using Microsoft.AspNetCore.Mvc;

namespace _211system.Areas.Fire.Controllers
{
    [Area("Fire")]
    public class TrucksController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }
    }
}