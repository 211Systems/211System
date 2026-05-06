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

        public IncidentsController(IIncidentService incidentService, _211DbContext context, IBlobStorageService blobStorageService)
        {
            _incidentService = incidentService;
            _context = context;
            _blobStorageService = blobStorageService;
        }

        [HttpPost]
        public async Task<ActionResult<IncidentDto>> CreateIncident([FromForm] CreateIncidentDto dto, IFormFile? photo)
        {
            Console.WriteLine($"Otrzymano: Desc={dto.Description}, SeverityId={dto.SeverityLevelId}");

            if (dto.SeverityLevelId == 0) 
                return BadRequest("Niepoprawny priorytet (ID=0)");
            try
            {
                if (photo != null && photo.Length > 0)
                {
                    var photoUrl = await _blobStorageService.UploadAsync(photo, "incidents");
                    dto.PhotoUrl = photoUrl;
                }

                var result = await _incidentService.CreateIncidentAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Błąd podczas tworzenia zgłoszenia.", error = ex.Message });
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

                var operations = await _context.MedicalOperations
                    .Where(o => o.ReportId == id)
                    .ToListAsync();

                if (operations.Any())
                {
                    _context.MedicalOperations.RemoveRange(operations);
                }

                var ambulances = await _context.Ambulances
                    .Where(a => a.CurrentIncidentId == id)
                    .ToListAsync();

                if (ambulances.Any())
                {
                    foreach (var amb in ambulances)
                    {
                        amb.CurrentIncidentId = null;
                        amb.IsAvailable = true;
                        
                        _context.Entry(amb).State = EntityState.Modified;
                    }
                }

                if (!string.IsNullOrEmpty(incident.PhotoUrl))
                {
                    try { await _blobStorageService.DeleteAsync(incident.PhotoUrl, "incidents"); } catch { }
                }

                _context.Incidents.Remove(incident);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Zgłoszenie usunięte, jednostki i akcje medyczne zwolnione.", 
                    releasedCount = ambulances.Count 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Błąd krytyczny.", error = ex.Message });
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