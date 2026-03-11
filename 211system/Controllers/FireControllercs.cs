using _211system.Models.Dtos.Fire;
using _211system.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;

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

        [HttpPost("departments")]
        public async Task<IActionResult> AddDepartment([FromBody] CreateFDepartmentDto dto)
        {
            var result = await _fireService.CreateDepartmentAsync(dto);
            return Ok(result);
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetAllDepartments()
        {
            var result = await _fireService.GetAllDepartmentsAsync();
            return Ok(result);
        }

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
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("firemen")]
        public async Task<IActionResult> GetAllFiremen()
        {
            var result = await _fireService.GetAllFiremenAsync();
            return Ok(result);
        }

        [HttpPost("firetrucks")]
        public async Task<IActionResult> AddFireTruck([FromBody] CreateFireTruckDto dto)
        {
            try
            {
                var result = await _fireService.CreateFireTruckAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("firetrucks")]
        public async Task<IActionResult> GetAllFireTrucks()
        {
            var result = await _fireService.GetAllFireTrucksAsync();
            return Ok(result);
        }
    }
}