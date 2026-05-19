using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _211system.Areas.Medic.Controllers
{
    [Area("Medic")]
    public class AviationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}