using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _211system.Areas.Police.Controllers
{
    [Area("Police")]
    public class AviationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}