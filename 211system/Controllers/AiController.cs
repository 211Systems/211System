using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _211system.Data;
using _211system.DTOs.Ai;
using _211system.Services;
using _211system.Models;
using _211system.Models.Interfaces;
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
        private readonly IWeatherService _weatherService;

        public AiController(_211DbContext context, IAiService aiService, IWeatherService weatherService)
        {
            _context = context;
            _aiService = aiService;
            _weatherService = weatherService;
        }

        [HttpPost("auto-dispatch")]
        public async Task<IActionResult> GenerateDispatchPlan()
        {

            var incidents = await _context.Incidents
                .Include(i => i.IncidentType)
                .Include(i => i.SeverityLevel)
                .Where(i => i.Status == "Nowe")
                .OrderBy(i => i.ReportDate)
                .Take(10)
                .Select(i => new AiIncidentDto
                {
                    Id = i.Id,
                    Description = i.Description,
                    Severity = i.SeverityLevel != null ? i.SeverityLevel.Name : "Brak",
                    IncidentType = i.IncidentType != null ? i.IncidentType.Name : "Nieznany",
                    Latitude = i.Latitude,
                    Longitude = i.Longitude
                })
                .ToListAsync();

            if (!incidents.Any())
                return Ok(new List<AiDispatchSuggestion>());


            var ambulances = await _context.Ambulances
                .Where(a => a.IsAvailable)
                .Select(a => new AiUnitDto { Id = a.Id, Name = a.LicensePlate, Latitude = a.Latitude, Longitude = a.Longitude })
                .ToListAsync();

            var fireTrucks = await _context.FireTrucks
                .Where(f => f.IsAvailable)
                .Select(f => new AiUnitDto { Id = f.Id, Name = f.LicensePlate, Latitude = f.Latitude, Longitude = f.Longitude })
                .ToListAsync();

            var policeCars = await _context.PoliceCars
                .Where(p => p.IsAvailable)
                .Select(p => new AiUnitDto { Id = p.Id, Name = p.LicensePlate, Latitude = p.Latitude, Longitude = p.Longitude })
                .ToListAsync();

            var medicalAir = await _context.AirUnits
                .Where(u => u.IsAvailable && u.ServiceType == ServiceType.Medical)
                .Select(u => new AiUnitDto { Id = u.Id, Name = u.Callsign, Latitude = u.Latitude, Longitude = u.Longitude })
                .ToListAsync();

            var policeAir = await _context.AirUnits
                .Where(u => u.IsAvailable && u.ServiceType == ServiceType.Police)
                .Select(u => new AiUnitDto { Id = u.Id, Name = u.Callsign, Latitude = u.Latitude, Longitude = u.Longitude })
                .ToListAsync();

            var fireAir = await _context.AirUnits
                .Where(u => u.IsAvailable && u.ServiceType == ServiceType.Fire)
                .Select(u => new AiUnitDto { Id = u.Id, Name = u.Callsign, Latitude = u.Latitude, Longitude = u.Longitude })
                .ToListAsync();

            bool anyUnitAvailable = ambulances.Any() || fireTrucks.Any() || policeCars.Any()
                                 || medicalAir.Any() || policeAir.Any() || fireAir.Any();

            if (!anyUnitAvailable)
                return Ok(new List<AiDispatchSuggestion>());

            var requestData = new AiDispatchRequestDto
            {
                Incidents = incidents,
                AvailableAmbulances = ambulances,
                AvailableFireTrucks = fireTrucks,
                AvailablePoliceCars = policeCars,
                AvailableMedicalAirUnits = medicalAir,
                AvailablePoliceAirUnits = policeAir,
                AvailableFireAirUnits = fireAir
            };

            // Pogoda na podstawie lokalizacji pierwszego incydentu
            if (incidents[0].Latitude != 0 || incidents[0].Longitude != 0)
            {
                try
                {
                    var groundWeather = await _weatherService.GetGroundConditionsAsync(
                        incidents[0].Latitude, incidents[0].Longitude);
                    var flightWeather = await _weatherService.GetFlightConditionsAsync(
                        incidents[0].Latitude, incidents[0].Longitude);

                    requestData.CurrentWeather = new AiWeatherDto
                    {
                        Temperature = groundWeather.Temperature,
                        Description = groundWeather.Description,
                        IsStormy = groundWeather.IsStormy,
                        IsFoggy = groundWeather.IsFoggy,
                        IsSlippery = groundWeather.IsSlippery,
                        VisibilityMeters = groundWeather.VisibilityMeters,
                        FlightRules = flightWeather.FlightRules,
                        IsFlightRecommended = flightWeather.IsFlightRecommended
                    };
                }
                catch
                {
                    // Pogoda niedostepna — AI poradzi sobie bez niej, nie blokujemy dyspozycji
                    requestData.CurrentWeather = null;
                }
            }

            var suggestions = await _aiService.GetAutoDispatchPlanAsync(requestData);
            return Ok(suggestions);
        }

        [HttpPost("confirm-dispatch")]
        public async Task<IActionResult> ConfirmDispatchPlan([FromBody] List<AiDispatchSuggestion> suggestions)
        {
            foreach (var sug in suggestions)
            {
                var incident = await _context.Incidents.FindAsync(sug.IncidentId);

                // Pomijamy jesli incydent nie istnieje lub juz nie jest "Nowy"
                if (incident == null || incident.Status != "Nowe") continue;

                incident.Status = "W toku";

                if (sug.UnitType == "Medical")
                {
                    var amb = await _context.Ambulances.FindAsync(sug.UnitId);
                    if (amb != null && amb.IsAvailable)
                    {
                        amb.IsAvailable = false;
                        amb.CurrentIncidentId = sug.IncidentId;
                    }
                    incident.IsMedicalActive = true;
                }
                else if (sug.UnitType == "Fire")
                {
                    var fire = await _context.FireTrucks.FindAsync(sug.UnitId);
                    if (fire != null && fire.IsAvailable)
                    {
                        fire.IsAvailable = false;
                        fire.CurrentIncidentId = sug.IncidentId;
                    }
                    incident.IsFireActive = true;
                }
                else if (sug.UnitType == "Police")
                {
                    var pol = await _context.PoliceCars.FindAsync(sug.UnitId);
                    if (pol != null && pol.IsAvailable)
                    {
                        pol.IsAvailable = false;
                        pol.CurrentIncidentId = sug.IncidentId;
                    }
                    incident.IsPoliceActive = true;
                }
                else if (sug.UnitType == "MedicalAir")
                {
                    var unit = await _context.AirUnits.FindAsync(sug.UnitId);
                    if (unit != null && unit.IsAvailable)
                    {
                        unit.IsAvailable = false;
                        unit.CurrentIncidentId = sug.IncidentId;
                    }
                    incident.IsMedicalActive = true;
                }
                else if (sug.UnitType == "PoliceAir")
                {
                    var unit = await _context.AirUnits.FindAsync(sug.UnitId);
                    if (unit != null && unit.IsAvailable)
                    {
                        unit.IsAvailable = false;
                        unit.CurrentIncidentId = sug.IncidentId;
                    }
                    incident.IsPoliceActive = true;
                }
                else if (sug.UnitType == "FireAir")
                {
                    var unit = await _context.AirUnits.FindAsync(sug.UnitId);
                    if (unit != null && unit.IsAvailable)
                    {
                        unit.IsAvailable = false;
                        unit.CurrentIncidentId = sug.IncidentId;
                    }
                    incident.IsFireActive = true;
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}