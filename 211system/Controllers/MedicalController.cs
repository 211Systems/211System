using _211system.DTOs.Hospital;
using _211system.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _211system.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicalController : Controller
    {
        private readonly IMedicalService _medicalService;

        public MedicalController(IMedicalService medicalService)
        {
            _medicalService = medicalService;
        }

        [Authorize(Roles = "Admin, Kierownik Szpitala")]
        [HttpPost("hospitals")]
        public async Task<IActionResult> CreateHospital([FromBody] CreateHospitalDto dto)
        {
            var result = await _medicalService.CreateHospitalAsync(dto);
            return Ok(result);
        }

        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz")]
        [HttpPost("paramedics")]
        public async Task<IActionResult> CreateParamedic([FromBody] CreateParamedicDto dto)
        {
            var result = await _medicalService.CreateParamedicAsync(dto);
            return Ok(result);
        }


        [Authorize(Roles = "Admin, Medyk")]
        [HttpPost("operations/start")]
        public async Task<IActionResult> StartOperation([FromQuery] Guid paramedicId, [FromQuery] Guid reportId)
        {
            try
            {
                var operationId = await _medicalService.StartMedicalOperationAsync(paramedicId, reportId);
                return Ok(new { Message = "Akcja rozpoczęta pomyślnie.", OperationId = operationId });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }
        //r

        [Authorize(Roles = "Admin, Medyk")]
        [HttpPut("operations/{operationId}/end")]
        public async Task<IActionResult> EndOperation(Guid operationId)
        {
            try
            {
                await _medicalService.EndMedicalOperationAsync(operationId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz, Medyk")]
        [HttpGet("hospitals")]
        public async Task<IActionResult> GetAllHospitals()
        {
            var result = await _medicalService.GetAllHospitalsAsync();
            return Ok(result);
        }

        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz, Medyk")]
        [HttpGet("paramedics")]
        public async Task<IActionResult> GetAllParamedics()
        {
            var result = await _medicalService.GetAllParamedicsAsync();
            return Ok(result);
        }

        [Authorize(Roles = "Admin, Kierownik Szpitala")]
        [HttpPost("ambulances")]
        public async Task<IActionResult> CreateAmbulance([FromBody] CreateAmbulanceDto dto)
        {
            var result = await _medicalService.CreateAmbulanceAsync(dto);
            return Ok(result);
        }

        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz, Medyk")]
        [HttpGet("ambulances")]
        public async Task<IActionResult> GetAllAmbulances()
        {
            var result = await _medicalService.GetAllAmbulancesAsync();
            return Ok(result);
        }
    }
}