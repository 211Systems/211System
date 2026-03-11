using _211system.DTOs.Hospital;
using _211system.Services;
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

        [HttpPost("hospitals")]
        public async Task<IActionResult> CreateHospital([FromBody] CreateHospitalDto dto)
        {
            var result = await _medicalService.CreateHospitalAsync(dto);
            return Ok(result);
        }

        [HttpPost("paramedics")]
        public async Task<IActionResult> CreateParamedic([FromBody] CreateParamedicDto dto)
        {
            var result = await _medicalService.CreateParamedicAsync(dto);
            return Ok(result);
        }


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
    }
}