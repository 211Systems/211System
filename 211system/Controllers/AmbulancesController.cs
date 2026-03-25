using Microsoft.AspNetCore.Mvc;

namespace _211system.Areas.Medic.Controllers
{
    [Area("Medic")]
    [Route("Medic/[controller]")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class AmbulancesController : Controller
    {
        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View();
        }
    }
}