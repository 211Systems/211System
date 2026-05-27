using System.Security.Claims;
using _211system.Data;
using _211system.DTOs.CPR112;
using _211system.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _211system.Models;
using _211system.Models.Interfaces;

namespace _211system.Controllers
{
    [Authorize(Roles = "Dyspozytor112, Admin112, Admin")]
    [ApiController]
    [Route("api/CPR112/[controller]")]
    public class IncidentsController : Controller
    {
        private readonly IIncidentService _incidentService;
        private readonly _211DbContext _context;
        private readonly IBlobStorageService _blobStorageService;
        private readonly IWeatherService _weatherService;

        public IncidentsController(IIncidentService incidentService, _211DbContext context, IBlobStorageService blobStorageService, IWeatherService weatherService)
        {
            _incidentService = incidentService;
            _context = context;
            _blobStorageService = blobStorageService;
            _weatherService = weatherService; 
        }

        [HttpPost]
        public async Task<ActionResult<IncidentDto>> CreateIncident([FromForm] CreateIncidentDto dto, IFormFile? photo)
        {
            Console.WriteLine($"Otrzymano: Desc={dto.Description}, SeverityId={dto.SeverityLevelId}");

            if (dto.SeverityLevelId == 0) 
                return BadRequest("Niepoprawny priorytet (ID=0)");
            try
            {
                var applicationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (applicationUserId != null)
                {
                    var currentOperator = await _context.Operators112
                        .FirstOrDefaultAsync(o => o.OpAccountId == applicationUserId);

                    dto.OperatorId = currentOperator?.Id;
                }
                else
                {
                    dto.OperatorId = null;
                }

                if (photo != null && photo.Length > 0)
                {
                    var photoUrl = await _blobStorageService.UploadAsync(photo, "incidents");
                    dto.PhotoUrl = photoUrl;
                }

                var result = await _incidentService.CreateIncidentAsync(dto);

                try
                {
                    var incidentEntity = await _context.Incidents.FindAsync(result.Id);
                    
                    if (incidentEntity != null)
                    {
                        double.TryParse(dto.Latitude?.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedLat);
                        double.TryParse(dto.Longitude?.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedLng);

                        var weather = await _weatherService.GetGroundConditionsAsync(parsedLat, parsedLng);
                        
                        incidentEntity.WeatherTemperature = weather.Temperature;
                        incidentEntity.WeatherCondition = weather.Description;
                        
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception wEx)
                {
                    Console.WriteLine($"[OSTRZEŻENIE] Nie udało się zapisać pogody dla zgłoszenia: {wEx.Message}");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"[BŁĄD CreateIncident] {detail}");
                return BadRequest(new { message = "Błąd podczas tworzenia zgłoszenia.", error = detail });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllIncidents()
        {
            var incidents = await _context.Incidents
                .Include(i => i.SeverityLevel)
                .Include(i => i.IncidentType)
                .OrderByDescending(i => i.ReportDate)
                .ToListAsync();

            var dtos = incidents.Select(inc => new IncidentDto
            {
                Id = inc.Id,
                IncidentNumber = inc.IncidentNumber,
                Description = inc.Description,
                Severity = inc.SeverityLevel != null ? inc.SeverityLevel.Name : "Brak",
                IncidentType = inc.IncidentType != null ? inc.IncidentType.Name : "Brak",
                Status = inc.Status,
                ReportedAt = inc.ReportDate,
                Latitude = inc.Latitude,
                Longitude = inc.Longitude,
                OperatorId = inc.OperatorId,
                PhotoUrl = string.IsNullOrEmpty(inc.PhotoUrl)
                    ? null
                    : _blobStorageService.GetSecureFileUrl(inc.PhotoUrl, "incidents")
            }).ToList();

            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<IncidentDto>> GetIncidentById(Guid id)
        {
            try
            {
                var result = await _incidentService.GetIncidentByIdAsync(id);
            
                if (!string.IsNullOrEmpty(result.PhotoUrl))
                {
                    result.PhotoUrl = _blobStorageService.GetSecureFileUrl(result.PhotoUrl, "incidents");
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> ChangeStatus(Guid id, [FromForm] ChangeIncidentStatusDto dto, IFormFile? newPhoto)
        {
            try
            {
                var ApplicationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (ApplicationUserId == null)
                    return Unauthorized("Brak autoryzacji.");

                var currentOperator = await _context.Operators112
                    .FirstOrDefaultAsync(o => o.OpAccountId == ApplicationUserId);

                Guid operatorId = currentOperator?.Id ?? Guid.Empty;

                if (newPhoto != null && newPhoto.Length > 0)
                {
                    var incident = await _context.Incidents.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
                    if (incident != null && !string.IsNullOrEmpty(incident.PhotoUrl))
                    {
                        await _blobStorageService.DeleteAsync(incident.PhotoUrl, "incidents");
                    }

                    dto.NewPhotoUrl = await _blobStorageService.UploadAsync(newPhoto, "incidents");
                }

                await _incidentService.ChangeIncidentStatusAsync(id, operatorId, dto);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin112, Admin")]
        public async Task<IActionResult> DeleteIncident(Guid id)
        {
            try
            {
                var incident = await _context.Incidents.FindAsync(id);
                if (incident == null) 
                    return NotFound(new { message = "Nie znaleziono zgłoszenia." });

                var medOps = await _context.MedicalOperations.Where(o => o.ReportId == id).ToListAsync();
                if (medOps.Any()) _context.MedicalOperations.RemoveRange(medOps);

                var polOps = await _context.PoliceOperations.Where(o => o.IncidentId == id).ToListAsync();
                if (polOps.Any()) _context.PoliceOperations.RemoveRange(polOps);

                var fireOps = await _context.FireOperations.Where(o => o.IncidentId == id).ToListAsync();
                if (fireOps.Any()) _context.FireOperations.RemoveRange(fireOps);

                var airOps = await _context.AviationOperations.Where(o => o.IncidentId == id).ToListAsync();
                if (airOps.Any()) _context.AviationOperations.RemoveRange(airOps);

                var ambulances = await _context.Ambulances.Where(a => a.CurrentIncidentId == id).ToListAsync();
                foreach (var amb in ambulances) { amb.CurrentIncidentId = null; amb.IsAvailable = true; }

                var policeCars = await _context.PoliceCars.Where(p => p.CurrentIncidentId == id).ToListAsync();
                foreach (var car in policeCars) { car.CurrentIncidentId = null; car.IsAvailable = true; }

                var fireTrucks = await _context.FireTrucks.Where(f => f.CurrentIncidentId == id).ToListAsync();
                foreach (var truck in fireTrucks) { truck.CurrentIncidentId = null; truck.IsAvailable = true; }

                var airUnits = await _context.AirUnits.Where(a => a.CurrentIncidentId == id).ToListAsync();
                foreach (var air in airUnits) { air.CurrentIncidentId = null; air.IsAvailable = true; }

                if (!string.IsNullOrEmpty(incident.PhotoUrl))
                {
                    try { await _blobStorageService.DeleteAsync(incident.PhotoUrl, "incidents"); } catch { }
                }

                _context.Incidents.Remove(incident);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Zgłoszenie usunięte, a wszystkie służby zostały zwolnione." });
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : "Brak szczegółów";
                return BadRequest(new { message = "Błąd krytyczny.", error = ex.Message, inner = innerMsg });
            }
        }

        [HttpGet("IncidentTypes")]
        public async Task<IActionResult> GetIncidentTypes()
        {
            var types = await _context.IncidentTypes
                .Select(t => new { t.Id, t.Name })
                .ToListAsync();
            return Ok(types);
        }

        [HttpGet("stats/summary")]
        public async Task<IActionResult> GetIncidentStats()
        {
            var stats = await _context.Incidents
                .GroupBy(i => i.IncidentType.Name)
                .Select(g => new { Name = g.Key ?? "Nieokreślone", Count = g.Count() })
                .ToListAsync();
            return Ok(stats);
        }

        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetIncidentHistory(Guid id)
        {
            var ApplicationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var history = await _context.IncidentStatusHistories
                .Where(h => h.IncidentId == id)
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new {
                    h.OldStatus,
                    h.NewStatus,
                    h.ChangedAt,
                    Operator = ApplicationUserId
                })
                .ToListAsync();

            return Ok(history);
        }
    } 
}