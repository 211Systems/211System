using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _211system.Areas.Fire.Controllers
{
    [Area("Fire")]
    public class AviationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}