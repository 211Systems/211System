using Microsoft.AspNetCore.Mvc;

namespace _211system.Controllers
{
    [Route("[controller]")]
    public class AuthViewController : Controller
    {
        [HttpGet("Login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet("Setup")]
        public IActionResult Setup()
        {
            return View();
        }
    }
}
