using Microsoft.AspNetCore.Mvc;
using _211system.Models.Dtos.Police;
using _211system.Models.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using _211system.Services;


namespace _211system.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PoliceController : Controller
    {
        private readonly IPoliceService _policeService;

        public PoliceController(IPoliceService policeService)
        {
            _policeService = policeService;
        }

        [Authorize(Roles = "Inspektor, Admin")]
        [HttpPost("departments")]
        public async Task<IActionResult> AddDepartment([FromBody] CreatePDepartmentDto dto)
        {
            var result = await _policeService.CreateDepartmentAsync(dto);
            return Ok(result);
        }

        [Authorize(Roles = "Admin, Inspektor, Komendant, Policjant")]
        [HttpGet("departments")]
        public async Task<IActionResult> GetAllDepartments()
        {
            var departments = await _policeService.GetAllDepartmentsAsync();
            return Ok(departments);
        } 

        [Authorize(Roles = "Komendant, Admin")]
        [HttpPost("policemen")]
        public async Task<IActionResult> AddPoliceman([FromBody] CreatePolicemanDto dto)
        {
            try
            {
                var result = await _policeService.CreatePolicemanAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin, Inspektor, Komendant, Policjant")]
        [HttpGet("policemen")]
        public async Task<IActionResult> GetAllPolicemen()
        {
            var policemen = await _policeService.GetAllPolicemenAsync();
            return Ok(policemen);
        }

        [Authorize(Roles = "Komendant, Admin")]
        [HttpDelete("policemen/{id}")]
        public async Task<IActionResult> DeletePoliceman(Guid id)
        {
            await _policeService.DeletePolicemanAsync(id);
            return Ok(new { message = "Zwolniono policjanta." });
        }

        [Authorize(Roles = "Komendant, Admin")]
        [HttpPost("cars")]
        public async Task<IActionResult> AddPoliceCar([FromBody] CreatePoliceCarDto dto)
        {
            try
            {
                var result = await _policeService.CreatePoliceCarAsync(dto);
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

        [Authorize(Roles = "Admin, Inspektor, Komendant, Policjant, Admin112, Dyspozytor112")]
        [HttpGet("cars")]
        public async Task<IActionResult> GetAllPoliceCars()
        {
            var cars = await _policeService.GetAllPoliceCarsAsync();
            return Ok(cars);
        }

        [Authorize(Roles = "Komendant, Admin")]
        [HttpPut("cars/{id}")]
        public async Task<IActionResult> UpdatePoliceCar(Guid id, [FromBody] UpdatePoliceCarDto dto)
        {
            try
            {
                await _policeService.UpdatePoliceCarAsync(id, dto);
                return Ok(new { message = "Zaktualizowano radiowóz." });
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

        [Authorize(Roles = "Komendant, Admin")]
        [HttpDelete("cars/{id}")]
        public async Task<IActionResult> DeletePoliceCar(Guid id)
        {
            await _policeService.DeletePoliceCarAsync(id);
            return Ok(new { message = "Usunięto radiowóz." });
        }

        [Authorize(Roles = "Admin, Admin112, Dyspozytor112")]
        [HttpPut("cars/{carId}/assign/{incidentId}")]
        public async Task<IActionResult> AssignPoliceCarToIncident(Guid carId, Guid incidentId)
        {
            try
            {
                await _policeService.AssignPoliceCarToIncidentAsync(carId, incidentId);
                return Ok(new { message = "Radiowóz został zadysponowany do zgłoszenia." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}