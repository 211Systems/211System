using _211system.Models.Dtos.Police;
using _211system.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;

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

        [HttpPost("departments")]
        public async Task<IActionResult> AddDepartment([FromBody] CreatePDepartmentDto dto)
        {
            var result = await _policeService.CreateDepartmentAsync(dto);
            return Ok(result);
        }

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
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("cars")]
        public async Task<IActionResult> AddPoliceCar([FromBody] CreatePoliceCarDto dto)
        {
            try
            {
                var result = await _policeService.CreatePoliceCarAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetAllDepartments()
        {
            var departments = await _policeService.GetAllDepartmentsAsync();
            return Ok(departments);
        }

        [HttpGet("policemen")]
        public async Task<IActionResult> GetAllPolicemen()
        {
            var policemen = await _policeService.GetAllPolicemenAsync();
            return Ok(policemen);
        }

        [HttpGet("cars")]
        public async Task<IActionResult> GetAllPoliceCars()
        {
            var cars = await _policeService.GetAllPoliceCarsAsync();
            return Ok(cars);
        }

    }
}
