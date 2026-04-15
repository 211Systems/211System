using Microsoft.AspNetCore.Mvc;

namespace _211system.Areas.Police.Controllers
{
    [Area("Police")]
    public class CarsController : Controller
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