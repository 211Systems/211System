using _211system.Models.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _211system.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weatherService;

        public WeatherController(IWeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        [HttpGet("incident/{lat}/{lng}")]
        // Tymczasowo bez autoryzacji, docelowo tylko dla ról związanych z zarządzaniem incydentami albo dla wszystkich, aby kadzy widział w swoim panelu
        //[Authorize(Roles = "Admin, Admin112, Dyspozytor112, Naczelnik, Komendant, Kierownik Szpitala")]
        public async Task<IActionResult> GetIncidentWeather(double lat, double lng)
        {
            try
            {
                var groundTask = _weatherService.GetGroundConditionsAsync(lat, lng);
                var flightTask = _weatherService.GetFlightConditionsAsync(lat, lng);

                await Task.WhenAll(groundTask, flightTask);

                return Ok(new
                {
                    Ground = groundTask.Result,
                    Aviation = flightTask.Result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Błąd pobierania danych meteo: " + ex.Message });
            }
        }
    }
}