using _211system.DTOs;
using _211system.Models;
using _211system.Models.Dtos.Aviation;
using _211system.Models.Interfaces;
using _211system.Models.Services;
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
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112, Inspektor, Komendant, Naczelnik, Kierownik Szpitala, Kapitan, Lekarz, Medyk, Policjant, Strazak")]
        public async Task<IActionResult> GetAirUnits()
        {
            var result = await _aviationService.GetAllAirUnitsAsync();
            return Ok(result);
        }

        [HttpPut("units/{unitId}/pilot")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112, Inspektor, Komendant, Naczelnik, Kierownik Szpitala, Kapitan")]
        public async Task<IActionResult> AssignPilot(Guid unitId, [FromBody] AssignPilotDto dto)
        {
            try
            {
                await _aviationService.AssignPilotAsync(unitId, dto.PilotId, dto.PilotName);
                return Ok(new { message = dto.PilotId.HasValue ? "Przypisano pilota do maszyny." : "Odpięto pilota od maszyny." });
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPut("units/{unitId}/assign/{incidentId}")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112")]
        public async Task<IActionResult> AssignUnit(Guid unitId, Guid incidentId)
        {
            try
            {
                await _aviationService.AssignAirUnitToIncidentAsync(unitId, incidentId);
                return Ok(new { message = "Jednostka powietrzna poderwana! Zapisano pogodę lotniczą przy zgłoszeniu." });
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("units/{unitId}/free")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112, Inspektor, Komendant, Naczelnik, Kierownik Szpitala, Kapitan, Lekarz, Medyk, Policjant, Strazak")]
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
                return Ok(new { message = "Usunięto wóz strażacki." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112, Inspektor, Komendant, Naczelnik, Kierownik Szpitala, Kapitan, Lekarz, Medyk, Policjant, Strazak")]
        public async Task<IActionResult> TransportPatient(Guid operationId, [FromBody] Guid hospitalId)
        {
            try
            {
                await _aviationService.TransportPatientAsync(operationId, hospitalId);
                return Ok(new { message = "Maszyna realizuje transport." });
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("operations/{operationId}/return")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112, Inspektor, Komendant, Naczelnik, Kierownik Szpitala, Kapitan, Lekarz, Medyk, Policjant, Strazak")]
        public async Task<IActionResult> ReturnToBase(Guid operationId)
        {
            try
            {
                await _aviationService.ReturnToBaseAsync(operationId);
                return Ok(new { message = "Maszyna wraca do bazy." });
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPut("operations/{operationId}/end")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112, Inspektor, Komendant, Naczelnik, Kierownik Szpitala, Kapitan, Lekarz, Medyk, Policjant, Strazak")]
        public async Task<IActionResult> EndOperation(Guid operationId)
        {
            try
            {
                await _aviationService.EndOperationAsync(operationId);
                return Ok(new { message = "Misja lotnicza zakończona." });
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}
