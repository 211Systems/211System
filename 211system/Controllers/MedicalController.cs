using _211system.DTOs.Hospital;
using _211system.Services;
using _211system.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _211system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MedicalController : ControllerBase
    {
        private readonly IMedicalService _medicalService;
        private readonly _211DbContext _context;

        public MedicalController(IMedicalService medicalService, _211DbContext context)
        {
            _medicalService = medicalService;
            _context = context;
        }

        [HttpPost("hospitals")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateHospital([FromBody] CreateHospitalDto dto)
        {
            try
            {
                var result = await _medicalService.CreateHospitalAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("hospitals")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz, Medyk, Admin112, Dyspozytor112")]
        public async Task<IActionResult> GetAllHospitals()
        {
            var result = await _medicalService.GetAllHospitalsAsync();
            return Ok(result);
        }

        [HttpPut("hospitals/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateHospital(Guid id, [FromBody] UpdateHospitalDto dto)
        {
            try
            {
                await _medicalService.UpdateHospitalAsync(id, dto);
                return Ok(new { message = "Zaktualizowano szpital." }); 
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("hospitals/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteHospital(Guid id)
        {
            await _medicalService.DeleteHospitalAsync(id);
            return Ok(new { message = "Usunięto szpital." });
        }

        [HttpPost("paramedics")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz")]
        public async Task<IActionResult> CreateParamedic([FromBody] CreateParamedicDto dto)
        {
            try
            {
                var result = await _medicalService.CreateParamedicAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("paramedics")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz, Medyk")]
        public async Task<IActionResult> GetAllParamedics()
        {
            var result = await _medicalService.GetAllParamedicsAsync();
            return Ok(result);
        }

        [HttpPut("paramedics/{id}")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz")]
        public async Task<IActionResult> UpdateParamedic(Guid id, [FromBody] UpdateParamedicDto dto)
        {
            try
            {
                await _medicalService.UpdateParamedicAsync(id, dto);
                return Ok(new { message = "Zaktualizowano dane pracownika." });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("paramedics/{id}")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz")]
        public async Task<IActionResult> DeleteParamedic(Guid id)
        {
            await _medicalService.DeleteParamedicAsync(id);
            return Ok(new { message = "Zwolniono pracownika." });
        }

        [HttpPost("operations/start")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz, Medyk, Admin112, Dyspozytor112")]
        public async Task<IActionResult> StartOperation([FromQuery] Guid paramedicId, [FromQuery] Guid reportId)
        {
            await _medicalService.StartMedicalOperationAsync(paramedicId, reportId);

            var incident = await _context.Incidents.FindAsync(reportId);
            if (incident != null)
            {
                incident.IsMedicalActive = true;
                incident.Status = "W toku";
                await _context.SaveChangesAsync();
            }

            return Ok("Rozpoczęto akcję medyczną.");
        }

        [HttpPut("operations/{operationId}/end")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz, Medyk")]
        public async Task<IActionResult> EndOperation(Guid operationId)
        {
            var op = await _context.MedicalOperations.FindAsync(operationId);
            if (op == null) return NotFound("Nie znaleziono operacji.");

            var incidentId = op.ReportId;

            await _medicalService.EndMedicalOperationAsync(operationId);

            var incident = await _context.Incidents.FindAsync(incidentId);
            if (incident != null)
            {
                incident.IsMedicalActive = false;

                if (!incident.IsPoliceActive && !incident.IsFireActive && !incident.IsMedicalActive)
                {
                    incident.Status = "Zakończone";
                }

                await _context.SaveChangesAsync();
            }

            return Ok("Zakończono akcję medyczną.");
        }

        [HttpGet("operations")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz, Medyk")]
        public async Task<IActionResult> GetAllOperations()
        {
            var result = await _medicalService.GetAllOperationsAsync();
            return Ok(result);
        }

        [HttpPost("ambulances")]
        [Authorize(Roles = "Admin, Kierownik Szpitala")]
        public async Task<IActionResult> CreateAmbulance([FromBody] CreateAmbulanceDto dto)
        {
            try
            {
                var result = await _medicalService.CreateAmbulanceAsync(dto);
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

        [HttpGet("ambulances")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz, Medyk, Admin112, Dyspozytor112")]
        public async Task<IActionResult> GetAllAmbulances()
        {
            var result = await _medicalService.GetAllAmbulancesAsync();
            return Ok(result);
        }

        [HttpPut("ambulances/{id}")]
        [Authorize(Roles = "Admin, Kierownik Szpitala")]
        public async Task<IActionResult> UpdateAmbulance(Guid id, [FromBody] UpdateAmbulanceDto dto)
        {
            try
            {
                await _medicalService.UpdateAmbulanceAsync(id, dto);
                return Ok(new { message = "Zaktualizowano karetkę." });
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

        [HttpDelete("ambulances/{id}")]
        [Authorize(Roles = "Admin, Kierownik Szpitala")]
        public async Task<IActionResult> DeleteAmbulance(Guid id)
        {
            await _medicalService.DeleteAmbulanceAsync(id);
            return Ok(new { message = "Usunięto karetkę." });
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
            try
            {
                await _medicalService.AssignAmbulanceToIncidentAsync(ambulanceId, incidentId);
                return Ok(new { message = "Karetka została zadysponowana do zgłoszenia." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("ambulances/{ambulanceId}/equipment")]
        [Authorize(Roles = "Admin, Kierownik Szpitala")]
        public async Task<IActionResult> AddEquipment(Guid ambulanceId, [FromBody] CreateAmbulanceEquipmentDto dto)
        {
            var result = await _medicalService.AddEquipmentAsync(ambulanceId, dto);
            return Ok(result);
        }

        [HttpGet("ambulances/{ambulanceId}/equipment")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz, Medyk, Admin112, Dyspozytor112")]
        public async Task<IActionResult> GetEquipment(Guid ambulanceId)
        {
            var result = await _medicalService.GetEquipmentAsync(ambulanceId);
            return Ok(result);
        }

        [HttpDelete("equipment/{id}")]
        [Authorize(Roles = "Admin, Kierownik Szpitala")]
        public async Task<IActionResult> DeleteEquipment(Guid id)
        {
            await _medicalService.DeleteEquipmentAsync(id);
            return Ok(new { message = "Usunięto sprzęt." });
        }
        [HttpGet("incidents/{id}")]
        [Authorize(Roles = "Admin, Kierownik Szpitala, Lekarz, Medyk")]
        public async Task<IActionResult> GetIncidentDetailsForMedic(Guid id)
        {
            var incidentDetails = await _context.Incidents
                .Where(i => i.Id == id)
                .Select(i => new IncidentDetailsMedicDto
                {
                    IncidentNumber = i.IncidentNumber,
                    Description = i.Description,
                    Severity = i.Severity,
                    Status = i.Status,
                    ReportDate = i.ReportDate,
                    Address = i.Location != null ? i.Location.Name + " (" + i.Location.Region + ")" : "Brak dokładnej lokalizacji"
                })
                .FirstOrDefaultAsync();

            if (incidentDetails == null) return NotFound(new { message = "Nie znaleziono zgłoszenia." });

            return Ok(incidentDetails);
        }
    }
}