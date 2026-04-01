using _211system.DTOs.Hospital;
using _211system.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _211system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MedicalController : Controller
    {
        private readonly IMedicalService _medicalService;

        public MedicalController(IMedicalService medicalService)
        {
            _medicalService = medicalService;
        }

        [HttpPost("hospitals")]
        [Authorize(Roles = "Admin, Kierownik Szpitala")]
        public async Task<IActionResult> CreateHospital([FromBody] CreateHospitalDto dto)
        {
            var result = await _medicalService.CreateHospitalAsync(dto);
            return Ok(result);
        }

        [HttpGet("hospitals")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz, Medyk, Admin112, Dyspozytor112")]
        public async Task<IActionResult> GetAllHospitals()
        {
            var result = await _medicalService.GetAllHospitalsAsync();
            return Ok(result);
        }

        [HttpPost("paramedics")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz")]
        public async Task<IActionResult> CreateParamedic([FromBody] CreateParamedicDto dto)
        {
            var result = await _medicalService.CreateParamedicAsync(dto);
            return Ok(result);
        }

        [HttpGet("paramedics")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz, Medyk")]
        public async Task<IActionResult> GetAllParamedics()
        {
            var result = await _medicalService.GetAllParamedicsAsync();
            return Ok(result);
        }

        [HttpPost("operations/start")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz, Medyk, Admin112, Dyspozytor112")]
        public async Task<IActionResult> StartOperation([FromQuery] Guid paramedicId, [FromQuery] Guid reportId)
        {
            await _medicalService.StartMedicalOperationAsync(paramedicId, reportId);
            return Ok("Rozpoczęto akcję medyczną.");
        }

        [HttpPut("operations/{operationId}/end")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz, Medyk")]
        public async Task<IActionResult> EndOperation(Guid operationId)
        {
            await _medicalService.EndMedicalOperationAsync(operationId);
            return Ok("Zakończono akcję medyczną.");
        }


        [HttpPost("ambulances")]
        [Authorize(Roles = "Admin, Kierownik Szpitala")]
        public async Task<IActionResult> CreateAmbulance([FromBody] CreateAmbulanceDto dto)
        {
            var result = await _medicalService.CreateAmbulanceAsync(dto);
            return Ok(result);
        }

        [HttpGet("ambulances")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz, Medyk, Admin112, Dyspozytor112")]
        public async Task<IActionResult> GetAllAmbulances()
        {
            var result = await _medicalService.GetAllAmbulancesAsync();
            return Ok(result);
        }

        [HttpGet("ambulances/available")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112, Kierownik Szpitala")]
        public async Task<IActionResult> GetAvailableAmbulances()
        {
            var result = await _medicalService.GetAvailableAmbulancesAsync();
            return Ok(result);
        }
        [HttpPut("ambulances/{ambulanceId}/assign/{incidentId}")]
        [Authorize(Roles = "Admin, Admin112, Dyspozytor112")]
        public async Task<IActionResult> AssignAmbulanceToIncident(Guid ambulanceId, Guid incidentId)
        {
            await _medicalService.AssignAmbulanceToIncidentAsync(ambulanceId, incidentId);
            return Ok("Karetka została zadysponowana do zgłoszenia.");
        }
    }
}