using _211system.DTOs;
using _211system.Models;
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
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112, Inspektor, Komendant, Naczelnik, Kierownik Szpitala")]
        public async Task<IActionResult> CreateAirbase([FromBody] CreateAirbaseDto dto)
        {
            var result = await _aviationService.CreateAirbaseAsync(dto);
            return Ok(result);
        }

        [HttpGet("airbases")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112, Inspektor, Komendant, Naczelnik, Kierownik Szpitala")]
        public async Task<IActionResult> GetAirbases()
        {
            var result = await _aviationService.GetAllAirbasesAsync();
            return Ok(result);
        }

        [HttpPost("units")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112, Inspektor, Komendant, Naczelnik, Kierownik Szpitala")]
        public async Task<IActionResult> CreateAirUnit([FromBody] CreateAirUnitDto dto)
        {
            try { return Ok(await _aviationService.CreateAirUnitAsync(dto)); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("units")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112, Inspektor, Komendant, Naczelnik, Kierownik Szpitala")]
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

        [HttpPost("units/{unitId}/free")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112, Inspektor, Komendant, Naczelnik, Kierownik Szpitala")]
        public async Task<IActionResult> FreeAirUnit(Guid unitId)
        {
            try
            {
                await _aviationService.FreeUnitAsync(unitId);
                return Ok(new { message = "Maszyna zwolniona i zawrócona do bazy." });
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpDelete("units/{unitId}")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112, Inspektor, Komendant, Naczelnik, Kierownik Szpitala")]
        public async Task<IActionResult> DeleteAirUnit(Guid unitId)
        {
            try
            {
                await _aviationService.DeleteAirUnitAsync(unitId);
                return Ok();
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPut("units/{unitId}")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112, Inspektor, Komendant, Naczelnik, Kierownik Szpitala")]
        public async Task<IActionResult> UpdateAirUnit(Guid unitId, [FromBody] UpdateAirUnitDto dto)
        {
            try
            {
                var result = await _aviationService.UpdateAirUnitAsync(unitId, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("units/{unitId}/location")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112, Inspektor, Komendant, Naczelnik, Kierownik Szpitala")]
        public async Task<IActionResult> UpdateLocation(Guid unitId, [FromBody] UpdateLocationDto dto)
        {
            try
            {
                await _aviationService.UpdateUnitLocationAsync(unitId, dto.Latitude, dto.Longitude, dto.Status);
                return Ok();
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("operations")]
        public async Task<IActionResult> GetActiveOperations() => Ok(await _aviationService.GetActiveOperationsAsync());

        [HttpPost("operations/{operationId}/transport")]
        public async Task<IActionResult> TransportPatient(Guid operationId, [FromBody] Guid hospitalId) => Ok();

        [HttpPost("operations/{operationId}/return")]
        public async Task<IActionResult> ReturnToBase(Guid operationId) => Ok();

        [HttpPut("operations/{operationId}/end")]
        public async Task<IActionResult> EndOperation(Guid operationId) => Ok();
    }
}
