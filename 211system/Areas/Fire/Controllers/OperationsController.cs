using Microsoft.AspNetCore.Mvc;

namespace _211system.Areas.Fire.Controllers
{
    [Area("Fire")]
    public class OperationsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}