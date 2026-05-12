using _211system.Models.Dtos.Aviation;
using _211system.Models.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _211system.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AviationController : ControllerBase
    {
        private readonly IAviationService _aviationService;

        public AviationController(IAviationService aviationService)
        {
            _aviationService = aviationService;
        }

        [HttpPost("airbases")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAirbase([FromBody] CreateAirbaseDto dto)
        {
            var result = await _aviationService.CreateAirbaseAsync(dto);
            return Ok(result);
        }

        [HttpGet("airbases")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112")]
        public async Task<IActionResult> GetAirbases()
        {
            var result = await _aviationService.GetAllAirbasesAsync();
            return Ok(result);
        }

        [HttpPost("units")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAirUnit([FromBody] CreateAirUnitDto dto)
        {
            try { return Ok(await _aviationService.CreateAirUnitAsync(dto)); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("units")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112")]
        public async Task<IActionResult> GetAirUnits()
        {
            var result = await _aviationService.GetAllAirUnitsAsync();
            return Ok(result);
        }

        [HttpPut("units/{unitId}/assign/{incidentId}")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112")]
        public async Task<IActionResult> AssignUnit(Guid unitId, Guid incidentId)
        {
            try
            {
                await _aviationService.AssignAirUnitToIncidentAsync(unitId, incidentId);
                return Ok(new { message = "Jednostka powietrzna poderwana!" });
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}