using Microsoft.AspNetCore.Mvc;

namespace _211system.Areas.Police.Controllers
{
    [Area("Police")]
    public class OperationsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}