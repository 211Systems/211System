using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace _211system.Areas.Dispatch.Controllers
{
    [Area("Dispatch")]
    [Route("Dispatch/[controller]")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class HomeController : Controller
    {
        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet("/Dispatch/Home/Report")]
        public IActionResult Report()
        {
            return View("~/Areas/Dispatch/Views/Home/Report.cshtml"); 
        }
    }
}
