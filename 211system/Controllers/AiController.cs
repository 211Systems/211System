using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _211system.Data;
using _211system.DTOs.Ai;
using _211system.Services;
using Microsoft.AspNetCore.Authorization;

namespace _211system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin, Admin112, Dyspozytor112")]
    public class AiController : ControllerBase
    {
        private readonly _211DbContext _context;
        private readonly IAiService _aiService;

        public AiController(_211DbContext context, IAiService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        [HttpPost("auto-dispatch")]
        public async Task<IActionResult> GenerateDispatchPlan()
        {
            var incidents = await _context.Incidents
                .Where(i => i.Status == "Nowe")
                .OrderBy(i => i.ReportDate)
                .Take(10)
                .Select(i => new AiIncidentDto
                {
                    Id = i.Id,
                    Description = i.Description,
                    Severity = i.SeverityLevelId.HasValue ? i.SeverityLevelId.ToString() : "Brak",
                    IncidentType = i.IncidentTypeId.HasValue ? i.IncidentTypeId.ToString() : "Brak"
                })
                .ToListAsync();

            if (!incidents.Any())
            {
                return Ok(new List<AiDispatchSuggestion>());
            }

            var requestData = new AiDispatchRequestDto
            {
                Incidents = incidents,
                AvailableAmbulances = await _context.Ambulances
                    .Where(a => a.IsAvailable)
                    .Select(a => new AiUnitDto { Id = a.Id, Name = a.LicensePlate })
                    .ToListAsync(),
                AvailableFireTrucks = await _context.FireTrucks
                    .Where(f => f.IsAvailable)
                    .Select(f => new AiUnitDto { Id = f.Id, Name = f.LicensePlate })
                    .ToListAsync(),
                AvailablePoliceCars = await _context.PoliceCars
                    .Where(p => p.IsAvailable)
                    .Select(p => new AiUnitDto { Id = p.Id, Name = p.LicensePlate })
                    .ToListAsync()
            };

            var suggestions = await _aiService.GetAutoDispatchPlanAsync(requestData);

            return Ok(suggestions);
        }

        [HttpPost("confirm-dispatch")]
        public async Task<IActionResult> ConfirmDispatchPlan([FromBody] List<AiDispatchSuggestion> suggestions)
        {
            foreach (var sug in suggestions)
            {

                var incident = await _context.Incidents.FindAsync(sug.IncidentId);

                if (incident == null || incident.Status != "Nowe") continue;

                if (incident != null) incident.Status = "W toku";

                if (sug.UnitType == "Medical")
                {
                    var amb = await _context.Ambulances.FindAsync(sug.UnitId);
                    if (amb != null && amb.IsAvailable) { amb.IsAvailable = false; amb.CurrentIncidentId = sug.IncidentId; }
                }
                else if (sug.UnitType == "Fire")
                {
                    var fire = await _context.FireTrucks.FindAsync(sug.UnitId);
                    if (fire != null && fire.IsAvailable) { fire.IsAvailable = false; fire.CurrentIncidentId = sug.IncidentId; }
                }
                else if (sug.UnitType == "Police")
                {
                    var pol = await _context.PoliceCars.FindAsync(sug.UnitId);
                    if (pol != null && pol.IsAvailable) { pol.IsAvailable = false; pol.CurrentIncidentId = sug.IncidentId; }
                }
                if (sug.UnitType == "Medical" && incident != null)
                    incident.IsMedicalActive = true;
                else if (sug.UnitType == "Fire" && incident != null)
                    incident.IsFireActive = true;
                else if (sug.UnitType == "Police" && incident != null)
                    incident.IsPoliceActive = true;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}