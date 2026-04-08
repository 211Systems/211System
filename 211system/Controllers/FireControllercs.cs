using Microsoft.AspNetCore.Mvc;
using _211system.Models.Dtos.Fire;
using _211system.Models.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace _211system.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FireController : Controller
    {
        private readonly IFireService _fireService;

        public FireController(IFireService fireService)
        {
            _fireService = fireService;
        }

        [Authorize(Roles = "Naczelnik, Admin")]
        [HttpPost("departments")]
        public async Task<IActionResult> AddDepartment([FromBody] CreateFDepartmentDto dto)
        {
            var result = await _fireService.CreateDepartmentAsync(dto);
            return Ok(result);
        }

        [Authorize(Roles = "Kapitan, Naczelnik, strazak, Admin")]
        [HttpGet("departments")]
        public async Task<IActionResult> GetAllDepartments()
        {
            var result = await _fireService.GetAllDepartmentsAsync();
            return Ok(result);
        }

        [Authorize(Roles = "Kapitan, Admin")]
        [HttpPost("firemen")]
        public async Task<IActionResult> AddFireman([FromBody] CreateFiremanDto dto)
        {
            try
            {
                var result = await _fireService.CreateFiremanAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Kapitan, Naczelnik, strazak, Admin")]
        [HttpGet("firemen")]
        public async Task<IActionResult> GetAllFiremen()
        {
            var result = await _fireService.GetAllFiremenAsync();
            return Ok(result);
        }

        [Authorize(Roles = "Kapitan, Admin")]
        [HttpDelete("firemen/{id}")]
        public async Task<IActionResult> DeleteFireman(Guid id)
        {
            await _fireService.DeleteFiremanAsync(id);
            return Ok(new { message = "Zwolniono strażaka." });
        }

        [Authorize(Roles = "Kapitan, Admin")]
        [HttpPost("firetrucks")]
        public async Task<IActionResult> AddFireTruck([FromBody] CreateFireTruckDto dto)
        {
            try
            {
                var result = await _fireService.CreateFireTruckAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Kapitan, Naczelnik, strazak, Admin, Admin112, Dyspozytor112")]
        [HttpGet("firetrucks")]
        public async Task<IActionResult> GetAllFireTrucks()
        {
            var result = await _fireService.GetAllFireTrucksAsync();
            return Ok(result);
        }

        [Authorize(Roles = "Kapitan, Admin")]
        [HttpPut("firetrucks/{id}")]
        public async Task<IActionResult> UpdateFireTruck(Guid id, [FromBody] UpdateFireTruckDto dto)
        {
            try
            {
                await _fireService.UpdateFireTruckAsync(id, dto);
                return Ok(new { message = "Zaktualizowano wóz strażacki." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Kapitan, Admin")]
        [HttpDelete("firetrucks/{id}")]
        public async Task<IActionResult> DeleteFireTruck(Guid id)
        {
            await _fireService.DeleteFireTruckAsync(id);
            return Ok(new { message = "Usunięto wóz strażacki." });
        }

        [Authorize(Roles = "Admin, Admin112, Dyspozytor112")]
        [HttpPut("firetrucks/{truckId}/assign/{incidentId}")]
        public async Task<IActionResult> AssignFireTruckToIncident(Guid truckId, Guid incidentId)
        {
            try
            {
                await _fireService.AssignFireTruckToIncidentAsync(truckId, incidentId);
                return Ok(new { message = "Wóz został zadysponowany do zgłoszenia." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}