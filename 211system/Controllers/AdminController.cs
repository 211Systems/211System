using _211system.Data;
using _211system.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _211system.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly _211DbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(_211DbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var incidents = await _context.Incidents.Select(i => new { i.Status, i.ReportDate, i.IncidentTypeId }).ToListAsync();
            var today = DateTime.UtcNow.Date;

            var ambTotal = await _context.Ambulances.CountAsync();
            var ambAvail = await _context.Ambulances.CountAsync(a => a.IsAvailable);
            var carTotal = await _context.PoliceCars.CountAsync();
            var carAvail = await _context.PoliceCars.CountAsync(c => c.IsAvailable);
            var truckTotal = await _context.FireTrucks.CountAsync();
            var truckAvail = await _context.FireTrucks.CountAsync(t => t.IsAvailable);
            var airTotal = await _context.AirUnits.CountAsync();
            var airAvail = await _context.AirUnits.CountAsync(u => u.IsAvailable);

            var incidentTypes = await _context.IncidentTypes.ToDictionaryAsync(t => t.Id, t => t.Name);
            var byType = incidents.Where(i => i.IncidentTypeId.HasValue)
                .GroupBy(i => i.IncidentTypeId.Value)
                .Select(g => new { type = incidentTypes.ContainsKey(g.Key) ? incidentTypes[g.Key] : "Inne", count = g.Count() })
                .OrderByDescending(x => x.count).ToList();

            return Ok(new
            {
                incidents = new
                {
                    total = incidents.Count,
                    nowe = incidents.Count(i => i.Status == "Nowe"),
                    wToku = incidents.Count(i => i.Status == "W toku"),
                    zakonczone = incidents.Count(i => i.Status == "Zakończone"),
                    falszywy = incidents.Count(i => i.Status == "Fałszywy alarm"),
                    dzis = incidents.Count(i => i.ReportDate.Date == today),
                    byType
                },
                vehicles = new
                {
                    ambulances = new { total = ambTotal, available = ambAvail, busy = ambTotal - ambAvail },
                    policeCars = new { total = carTotal, available = carAvail, busy = carTotal - carAvail },
                    fireTrucks = new { total = truckTotal, available = truckAvail, busy = truckTotal - truckAvail },
                    airUnits = new { total = airTotal, available = airAvail, busy = airTotal - airAvail }
                },
                personnel = new
                {
                    paramedics = await _context.Paramedics.CountAsync(),
                    policemen = await _context.Policemen.CountAsync(),
                    firemen = await _context.Firemen.CountAsync(),
                    operators = await _context.Operators112.CountAsync()
                },
                facilities = new
                {
                    hospitals = await _context.Hospitals.CountAsync(),
                    policeDepartments = await _context.PoliceDepartments.CountAsync(),
                    fireDepartments = await _context.FireDepartments.CountAsync(),
                    airbases = await _context.Airbases.CountAsync(),
                    cprCenters = await _context.Encs.CountAsync()
                },
                activeOperations = new
                {
                    police = await _context.PoliceOperations.CountAsync(o => o.EndTime == null),
                    fire = await _context.FireOperations.CountAsync(o => o.EndTime == null),
                    medical = await _context.MedicalOperations.CountAsync(o => o.EndTime == null),
                    aviation = await _context.AviationOperations.CountAsync(o => o.EndTime == null)
                }
            });
        }

        [HttpGet("live")]
        public async Task<IActionResult> GetLive()
        {
            var activeIncidents = await _context.Incidents
                .Include(i => i.IncidentType)
                .Include(i => i.SeverityLevel)
                .Where(i => i.Status != "Zakończone" && i.Status != "Fałszywy alarm")
                .OrderByDescending(i => i.ReportDate)
                .ToListAsync();

            var ids = activeIncidents.Select(i => i.Id).ToList();

            var attachmentCounts = await _context.Attachments
                .Where(a => ids.Contains(a.IncidentId))
                .GroupBy(a => a.IncidentId)
                .Select(g => new { IncidentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.IncidentId, x => x.Count);

            var polCount = await _context.PoliceOperations
                .Where(o => o.EndTime == null && ids.Contains(o.IncidentId)).GroupBy(o => o.IncidentId).Select(g => new { g.Key, c = g.Count() }).ToListAsync();

            var fireCount = await _context.FireOperations
                .Where(o => o.EndTime == null && ids.Contains(o.IncidentId)).GroupBy(o => o.IncidentId).Select(g => new { g.Key, c = g.Count() }).ToListAsync();

            var medCount = await _context.MedicalOperations
                .Where(o => o.EndTime == null && ids.Contains(o.ReportId)).GroupBy(o => o.ReportId).Select(g => new { g.Key, c = g.Count() }).ToListAsync();

            var airOps = await _context.AviationOperations.Include(o => o.AirUnit)
                .Where(o => o.EndTime == null && o.IncidentId.HasValue && ids.Contains(o.IncidentId.Value)).ToListAsync();

            var incidents = activeIncidents.Select(i => new
            {
                i.Id,
                i.IncidentNumber,
                i.Status,
                type = i.IncidentType != null ? i.IncidentType.Name : "Brak",
                severity = i.SeverityLevel != null ? i.SeverityLevel.Name : "-",
                reportedAt = i.ReportDate,
                i.Latitude,
                i.Longitude,
                police = polCount.FirstOrDefault(x => x.Key == i.Id)?.c ?? 0,
                fire = fireCount.FirstOrDefault(x => x.Key == i.Id)?.c ?? 0,
                medical = medCount.FirstOrDefault(x => x.Key == i.Id)?.c ?? 0,
                aviation = airOps.Count(x => x.IncidentId == i.Id),
                attachmentCount = attachmentCounts.TryGetValue(i.Id, out var ac) ? ac : 0
            }).ToList();

            var busyAir = airOps.Select(o => new
            {
                callsign = o.AirUnit?.Callsign,
                pilot = string.IsNullOrEmpty(o.AirUnit?.PilotName) ? "brak pilota" : o.AirUnit.PilotName,
                service = o.AirUnit != null ? o.AirUnit.ServiceType.ToString() : "",
                incidentId = o.IncidentId
            }).ToList();

            return Ok(new { incidents, busyAir });
        }

        [HttpGet("diagnostics")]
        public async Task<IActionResult> GetDiagnostics()
        {
            bool dbOk;
            try { dbOk = await _context.Database.CanConnectAsync(); } catch { dbOk = false; }

            var ambStuck = await _context.Ambulances.CountAsync(a => !a.IsAvailable && a.CurrentIncidentId == null);
            var carStuck = await _context.PoliceCars.CountAsync(c => !c.IsAvailable && c.CurrentIncidentId == null);
            var truckStuck = await _context.FireTrucks.CountAsync(t => !t.IsAvailable && t.CurrentIncidentId == null);
            var airStuck = await _context.AirUnits.CountAsync(u => !u.IsAvailable && u.CurrentIncidentId == null);

            var inProgress = await _context.Incidents.Where(i => i.Status == "W toku").Select(i => i.Id).ToListAsync();
            var withPol = await _context.PoliceOperations.Where(o => o.EndTime == null).Select(o => o.IncidentId).Distinct().ToListAsync();
            var withFire = await _context.FireOperations.Where(o => o.EndTime == null).Select(o => o.IncidentId).Distinct().ToListAsync();
            var withMed = await _context.MedicalOperations.Where(o => o.EndTime == null).Select(o => o.ReportId).Distinct().ToListAsync();
            var withAir = await _context.AviationOperations.Where(o => o.EndTime == null && o.IncidentId.HasValue).Select(o => o.IncidentId.Value).Distinct().ToListAsync();
            var orphanIncidents = inProgress.Count(id => !withPol.Contains(id) && !withFire.Contains(id) && !withMed.Contains(id) && !withAir.Contains(id));

            var roleStats = new List<object>();
            foreach (var role in _roleManager.Roles.ToList())
            {
                var count = (await _userManager.GetUsersInRoleAsync(role.Name)).Count;
                roleStats.Add(new { role = role.Name, users = count });
            }

            return Ok(new
            {
                database = dbOk ? "OK" : "BŁĄD POŁĄCZENIA",
                serverTimeUtc = DateTime.UtcNow,
                totalUsers = _userManager.Users.Count(),
                stuckVehicles = new { ambulances = ambStuck, policeCars = carStuck, fireTrucks = truckStuck, airUnits = airStuck },
                orphanInProgressIncidents = orphanIncidents,
                roles = roleStats
            });
        }
    }
}
