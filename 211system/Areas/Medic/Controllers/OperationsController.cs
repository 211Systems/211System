using Microsoft.AspNetCore.Mvc;

namespace _211system.Areas.Medic.Controllers
{
    [Area("Medic")]
    public class OperationsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}