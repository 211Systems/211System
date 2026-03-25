using Microsoft.AspNetCore.Mvc;

namespace _211system.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class CprCentersController : Controller
    {
        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index() { 
            return View();
        }

        [HttpGet("Create")]
        public IActionResult Create() { 
            return View();
        }

        [HttpGet("Details/{id}")]
        public IActionResult Details(string id)
        {
            ViewBag.CprCenterId = id;
            return View();
        }
    }
}