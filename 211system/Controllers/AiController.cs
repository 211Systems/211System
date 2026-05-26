using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _211system.Data;
using _211system.DTOs.Ai;
using _211system.Services;
using _211system.Models;
using _211system.Models.Aviation;
using _211system.Models.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using FireDepartment;
using Police;

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
        private readonly ILogger<AiController> _logger;

        public AiController(
            _211DbContext context,
            IAiService aiService,
            IWeatherService weatherService,
            ILogger<AiController> logger)
        {
            _context = context;
            _aiService = aiService;
            _weatherService = weatherService;
            _logger = logger;
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
                    requestData.CurrentWeather = null;
                }
            }

            List<AiDispatchSuggestion> suggestions;
            try
            {
                suggestions = await _aiService.GetAutoDispatchPlanAsync(requestData);
            }
            catch (AiServiceUnavailableException ex)
            {
                _logger.LogWarning(ex,
                    "Model AI niedostępny (kod upstream: {Upstream}).",
                    ex.UpstreamStatusCode);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = ex.Message,
                    retryable = true,
                    upstreamStatus = ex.UpstreamStatusCode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas wywołania AI.");
                return StatusCode(500, new { message = "Błąd modelu AI: " + ex.Message });
            }

            return Ok(suggestions);
        }

        [HttpPost("confirm-dispatch")]
        public async Task<IActionResult> ConfirmDispatchPlan([FromBody] List<AiDispatchSuggestion> suggestions)
        {
            if (suggestions == null || suggestions.Count == 0)
                return BadRequest(new { message = "Brak propozycji do zatwierdzenia." });

            var assigned = new List<object>();
            var skipped = new List<object>();

            foreach (var sug in suggestions)
            {
                try
                {
                    var incident = await _context.Incidents.FindAsync(sug.IncidentId);
                    if (incident == null)
                    {
                        skipped.Add(new { sug.IncidentId, sug.UnitId, sug.UnitType, reason = "Incydent nie istnieje." });
                        continue;
                    }

                    if (incident.Status != "Nowe" && incident.Status != "W toku")
                    {
                        skipped.Add(new { sug.IncidentId, sug.UnitId, sug.UnitType, reason = $"Incydent jest w stanie '{incident.Status}'." });
                        continue;
                    }

                    bool unitAssigned = false;

                    switch (sug.UnitType)
                    {
                        case "Medical":
                            unitAssigned = await TryAssignAmbulanceAsync(sug, incident);
                            if (unitAssigned) incident.IsMedicalActive = true;
                            break;

                        case "Fire":
                            unitAssigned = await TryAssignFireTruckAsync(sug, incident);
                            if (unitAssigned) incident.IsFireActive = true;
                            break;

                        case "Police":
                            unitAssigned = await TryAssignPoliceCarAsync(sug, incident);
                            if (unitAssigned) incident.IsPoliceActive = true;
                            break;

                        case "MedicalAir":
                            unitAssigned = await TryAssignAirUnitAsync(sug, incident);
                            if (unitAssigned) incident.IsMedicalActive = true;
                            break;

                        case "PoliceAir":
                            unitAssigned = await TryAssignAirUnitAsync(sug, incident);
                            if (unitAssigned) incident.IsPoliceActive = true;
                            break;

                        case "FireAir":
                            unitAssigned = await TryAssignAirUnitAsync(sug, incident);
                            if (unitAssigned) incident.IsFireActive = true;
                            break;

                        default:
                            skipped.Add(new { sug.IncidentId, sug.UnitId, sug.UnitType, reason = $"Nieznany typ jednostki: '{sug.UnitType}'." });
                            continue;
                    }

                    if (!unitAssigned)
                    {
                        skipped.Add(new { sug.IncidentId, sug.UnitId, sug.UnitType, reason = "Jednostka nie istnieje lub jest niedostępna." });
                        continue;
                    }

                    if (incident.Status == "Nowe")
                    {
                        var oldStatus = incident.Status;
                        incident.Status = "W toku";
                        _context.IncidentStatusHistories.Add(new IncidentStatusHistory
                        {
                            IncidentId = incident.Id,
                            OldStatus = oldStatus,
                            NewStatus = "W toku",
                            ChangedAt = DateTime.UtcNow
                        });
                    }

                    assigned.Add(new { sug.IncidentId, sug.UnitId, sug.UnitType });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Bład przy przygotowywaniu dyspozycji AI dla incydentu {IncidentId}, jednostka {UnitId} ({UnitType}).",
                        sug.IncidentId, sug.UnitId, sug.UnitType);
                    skipped.Add(new { sug.IncidentId, sug.UnitId, sug.UnitType, reason = "Wewnętrzny błąd przygotowania: " + ex.Message });
                }
            }

            if (assigned.Count == 0)
            {
                return BadRequest(new
                {
                    message = "Nie udało się przypisać żadnej jednostki.",
                    assigned,
                    skipped
                });
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                var detail = dbEx.InnerException?.Message ?? dbEx.Message;
                _logger.LogError(dbEx,
                    "Bład zapisu dyspozycji AI do bazy. Szczegóły: {Detail}", detail);
                return StatusCode(500, new
                {
                    message = "Błąd podczas zapisu dyspozycji do bazy: " + detail,
                    assigned,
                    skipped
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd przy zapisie dyspozycji AI.");
                return StatusCode(500, new
                {
                    message = "Nieoczekiwany błąd: " + ex.Message,
                    assigned,
                    skipped
                });
            }

            return Ok(new
            {
                message = $"Zadysponowano {assigned.Count} jednostek (pominięto {skipped.Count}).",
                assigned,
                skipped
            });
        }

        private async Task<bool> TryAssignAmbulanceAsync(AiDispatchSuggestion sug, CPR112.Models.Incident incident)
        {
            var amb = await _context.Ambulances.FindAsync(sug.UnitId);
            if (amb == null || !amb.IsAvailable) return false;

            amb.IsAvailable = false;
            amb.CurrentIncidentId = sug.IncidentId;
            amb.Status = VehicleOperationalStatus.EnRouteToIncident;

            if (amb.ParamedicId.HasValue)
            {
                _context.MedicalOperations.Add(new _211system.Models.Hospital.MedicalOperation
                {
                    ReportId = incident.Id,
                    ParamedicId = amb.ParamedicId,
                    StartTime = DateTime.UtcNow
                });
            }

            return true;
        }

        private async Task<bool> TryAssignFireTruckAsync(AiDispatchSuggestion sug, CPR112.Models.Incident incident)
        {
            var truck = await _context.FireTrucks.FindAsync(sug.UnitId);
            if (truck == null || !truck.IsAvailable) return false;

            truck.IsAvailable = false;
            truck.CurrentIncidentId = sug.IncidentId;
            truck.Status = VehicleOperationalStatus.EnRouteToIncident;

            _context.FireOperations.Add(new FireDepartmentOperation
            {
                FDepartmentId = truck.FDepartmentId,
                IncidentId = incident.Id,
                FiremanId = truck.FiremanId,
                StartTime = DateTime.UtcNow
            });

            return true;
        }

        private async Task<bool> TryAssignPoliceCarAsync(AiDispatchSuggestion sug, CPR112.Models.Incident incident)
        {
            var car = await _context.PoliceCars.FindAsync(sug.UnitId);
            if (car == null || !car.IsAvailable) return false;

            car.IsAvailable = false;
            car.CurrentIncidentId = sug.IncidentId;
            car.Status = VehicleOperationalStatus.EnRouteToIncident;

            _context.PoliceOperations.Add(new PoliceOperation
            {
                PDepartmentId = car.PDepartmentId,
                IncidentId = incident.Id,
                PolicemanId = car.PolicemanId,
                StartTime = DateTime.UtcNow
            });

            return true;
        }

        private async Task<bool> TryAssignAirUnitAsync(AiDispatchSuggestion sug, CPR112.Models.Incident incident)
        {
            var unit = await _context.AirUnits.FindAsync(sug.UnitId);
            if (unit == null || !unit.IsAvailable) return false;

            unit.IsAvailable = false;
            unit.CurrentIncidentId = sug.IncidentId;
            unit.Status = VehicleOperationalStatus.EnRouteToIncident;

            _context.AviationOperations.Add(new AviationOperation
            {
                AirUnitId = unit.Id,
                IncidentId = incident.Id,
                StartTime = DateTime.UtcNow
            });

            return true;
        }
    }
}