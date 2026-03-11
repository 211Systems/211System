using _211system.Models.Interfaces;
using _211system.Services;
using Microsoft.AspNetCore.Mvc;

namespace _211system.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NasaController : ControllerBase
{
    private readonly INasaService _nasaService;

    public NasaController(INasaService nasaService)
    {
        _nasaService = nasaService;
    }

    [HttpPost("fetch")]
    public async Task<IActionResult> FetchNasaData()
    {
        try
        {
            var result = await _nasaService.FetchFireDataAndCreateIncidentsAsync();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest($"Wystąpił błąd: {ex.Message}");
        }
    }
}