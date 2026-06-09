using _211system.Data;
using _211system.DTOs;
using _211system.Models;
using _211system.Models.Aviation;
using _211system.Models.Dtos.Aviation;
using _211system.Models.Interfaces;
using CPR112.Models;
using Microsoft.EntityFrameworkCore;

namespace _211system.Services
{
    public class AviationService : IAviationService
    {
        private readonly _211DbContext _context;
        private readonly ITransportService _transportService;
        private readonly IWeatherService _weatherService;

        public AviationService(_211DbContext context, ITransportService transportService, IWeatherService weatherService)
        {
            _context = context;
            _transportService = transportService;
            _weatherService = weatherService;
        }

        public async Task<Airbase> CreateAirbaseAsync(CreateAirbaseDto dto)
        {
            var airbase = new Airbase
            {
                Name = dto.Name,
                IcaoCode = dto.IcaoCode,
                ServiceType = dto.ServiceType,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude
            };

            await _context.Airbases.AddAsync(airbase);
            await _context.SaveChangesAsync();

            await HelipadHelper.SyncDepartmentHelipadAsync(_context, dto.ServiceType, dto.Latitude, dto.Longitude);

            return airbase;
        }

        public async Task<IEnumerable<Airbase>> GetAllAirbasesAsync()
        {
            return await _context.Airbases.ToListAsync();
        }

        public async Task<AirUnit> CreateAirUnitAsync(CreateAirUnitDto dto)
        {
            var airbase = await _context.Airbases.FindAsync(dto.AirbaseId);
            if (airbase == null) throw new ArgumentException("Baza lotnicza nie istnieje.");

            var unit = new AirUnit
            {
                Callsign = dto.Callsign,
                Type = dto.Type,
                ServiceType = dto.ServiceType,
                AirbaseId = dto.AirbaseId,
                Latitude = airbase.Latitude,
                Longitude = airbase.Longitude,
                IsAvailable = true,
                Status = VehicleOperationalStatus.InBase
            };

            await _context.AirUnits.AddAsync(unit);
            await _context.SaveChangesAsync();
            return unit;
        }

        public async Task<IEnumerable<AirUnitDto>> GetAllAirUnitsAsync()
        {
            var units = await _context.AirUnits.ToListAsync();
            var airbases = await _context.Airbases.ToDictionaryAsync(a => a.Id, a => a.Name);
            var crews = await _context.VehicleCrews.Where(c => c.VehicleType == "air").ToListAsync();

            return units.Select(u => new AirUnitDto
            {
                Id = u.Id,
                Callsign = u.Callsign,
                Type = (int)u.Type,
                ServiceType = (int)u.ServiceType,
                IsAvailable = u.IsAvailable,
                Status = (int)u.Status,
                Latitude = u.Latitude,
                Longitude = u.Longitude,
                AirbaseId = u.AirbaseId,
                AirbaseName = airbases.TryGetValue(u.AirbaseId, out var bn) ? bn : "Brak bazy",
                CurrentIncidentId = u.CurrentIncidentId,
                PilotId = u.PilotId,
                PilotName = u.PilotName,
                Crew = crews.Where(c => c.VehicleId == u.Id)
                            .Select(c => new AirCrewMemberDto { MemberId = c.MemberId, MemberName = c.MemberName })
                            .ToList()
            });
        }

        public async Task AssignPilotAsync(Guid unitId, Guid? pilotId, string pilotName)
        {
            var unit = await _context.AirUnits.FindAsync(unitId);
            if (unit == null) throw new ArgumentException("Maszyna nie istnieje.");

            unit.PilotId = pilotId;
            unit.PilotName = pilotId.HasValue ? pilotName : null;

            _context.AirUnits.Update(unit);
            await _context.SaveChangesAsync();
        }

        public async Task AssignAirUnitToIncidentAsync(Guid unitId, Guid incidentId)
        {
            var unit = await _context.AirUnits.FindAsync(unitId);
            if (unit == null || !unit.IsAvailable) throw new InvalidOperationException("Jednostka jest niedostępna.");

            unit.IsAvailable = false;
            unit.CurrentIncidentId = incidentId;
            unit.Status = VehicleOperationalStatus.EnRouteToIncident;

            var incident = await _context.Incidents.FindAsync(incidentId);
            if (incident != null)
            {
                if (incident.Status == "Nowe")
                    incident.Status = "W toku";

                await AppendAviationWeatherAsync(incident);
                _context.Incidents.Update(incident);
            }

            _context.AirUnits.Update(unit);

            var operation = new AviationOperation
            {
                AirUnitId = unit.Id,
                IncidentId = incidentId,
                StartTime = DateTime.UtcNow
            };
            await _context.AviationOperations.AddAsync(operation);

            await _context.SaveChangesAsync();
        }

        public async Task FreeUnitAsync(Guid unitId)
        {
            var unit = await _context.AirUnits.FindAsync(unitId);
            if (unit == null)
                throw new Exception("Nie znaleziono jednostki lotniczej.");

            var incidentId = unit.CurrentIncidentId;
            var svc = unit.ServiceType;

            unit.IsAvailable = true;
            unit.Status = VehicleOperationalStatus.InBase;
            unit.CurrentIncidentId = null;

            var baseDb = await _context.Airbases.FindAsync(unit.AirbaseId);
            if (baseDb != null)
            {
                unit.Latitude = baseDb.Latitude;
                unit.Longitude = baseDb.Longitude;
            }

            _context.AirUnits.Update(unit);

            var activeOperation = await _context.AviationOperations
                .Where(o => o.AirUnitId == unitId && o.EndTime == null)
                .FirstOrDefaultAsync();

            if (activeOperation != null)
            {
                activeOperation.EndTime = DateTime.UtcNow;
                _context.AviationOperations.Update(activeOperation);
            }

            if (incidentId.HasValue)
            {
                await CloseIncidentIfDoneAsync(incidentId.Value, unitId, svc);
            }

            await _context.SaveChangesAsync();
        }

        private async Task CloseIncidentIfDoneAsync(Guid incidentId, Guid freedUnitId, ServiceType freedService)
        {
            var incident = await _context.Incidents.FindAsync(incidentId);
            if (incident == null || incident.Status == "Zakończone") return;

            var activeAirUnitIds = await _context.AviationOperations
                .Where(o => o.IncidentId == incidentId && o.EndTime == null && o.AirUnitId != freedUnitId)
                .Select(o => o.AirUnitId)
                .ToListAsync();
            var activeAirServices = await _context.AirUnits
                .Where(a => activeAirUnitIds.Contains(a.Id))
                .Select(a => a.ServiceType)
                .ToListAsync();
            bool otherAirActive = activeAirUnitIds.Count > 0;

            bool medGround = await _context.MedicalOperations.AnyAsync(o => o.ReportId == incidentId && o.EndTime == null);
            bool polGround = await _context.PoliceOperations.AnyAsync(o => o.IncidentId == incidentId && o.EndTime == null);
            bool fireGround = await _context.FireOperations.AnyAsync(o => o.IncidentId == incidentId && o.EndTime == null);

            if (freedService == ServiceType.Medical && !medGround && !activeAirServices.Contains(ServiceType.Medical))
                incident.IsMedicalActive = false;
            if (freedService == ServiceType.Police && !polGround && !activeAirServices.Contains(ServiceType.Police))
                incident.IsPoliceActive = false;
            if (freedService == ServiceType.Fire && !fireGround && !activeAirServices.Contains(ServiceType.Fire))
                incident.IsFireActive = false;

            bool anyActive = incident.IsPoliceActive || incident.IsFireActive || incident.IsMedicalActive
                             || otherAirActive || medGround || polGround || fireGround;

            if (!anyActive)
            {
                var old = incident.Status;
                incident.Status = "Zakończone";
                incident.IsPoliceActive = false;
                incident.IsFireActive = false;
                incident.IsMedicalActive = false;
                _context.IncidentStatusHistories.Add(new IncidentStatusHistory
                {
                    IncidentId = incident.Id,
                    OldStatus = old,
                    NewStatus = "Zakończone",
                    ChangedAt = DateTime.UtcNow
                });
            }
            _context.Incidents.Update(incident);
        }

        public async Task DeleteAirUnitAsync(Guid unitId)
        {
            var unit = await _context.AirUnits.FindAsync(unitId);
            if (unit == null) return;

            if (!unit.IsAvailable || unit.CurrentIncidentId.HasValue)
                throw new InvalidOperationException("Nie można wyrejestrować maszyny - jest w misji. Najpierw zakończ misję lub użyj „zwróć maszynę”.");
            if (unit != null)
            {
                _context.AirUnits.Remove(unit);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<AirUnit> UpdateAirUnitAsync(Guid unitId, UpdateAirUnitDto dto)
        {
            var unit = await _context.AirUnits.FindAsync(unitId);
            if (unit == null) throw new ArgumentException("Maszyna nie istnieje.");

            var airbase = await _context.Airbases.FindAsync(dto.AirbaseId);
            if (airbase == null) throw new ArgumentException("Baza lotnicza nie istnieje.");

            if (airbase.ServiceType != unit.ServiceType)
                throw new ArgumentException("Baza musi należeć do tej samej służby.");

            unit.Callsign = dto.Callsign;
            unit.Type = dto.Type;
            unit.AirbaseId = dto.AirbaseId;

            if (unit.IsAvailable)
            {
                unit.Latitude = airbase.Latitude;
                unit.Longitude = airbase.Longitude;
            }

            _context.AirUnits.Update(unit);
            await _context.SaveChangesAsync();
            return unit;
        }

        public async Task UpdateUnitLocationAsync(Guid unitId, double lat, double lng, int? statusId)
        {
            var unit = await _context.AirUnits.FindAsync(unitId);
            if (unit != null)
            {
                unit.Latitude = lat;
                unit.Longitude = lng;

                if (statusId.HasValue)
                {
                    unit.Status = (VehicleOperationalStatus)statusId.Value;
                }

                _context.AirUnits.Update(unit);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<dynamic>> GetActiveOperationsAsync()
        {
            return await _context.AviationOperations
                .Include(o => o.AirUnit)
                .Select(o => new
                {
                    Id = o.Id,
                    AirUnitId = o.AirUnitId,
                    IncidentId = o.IncidentId,
                    StartTime = o.StartTime,
                    EndTime = o.EndTime
                })
                .ToListAsync();
        }

        public async Task TransportPatientAsync(Guid operationId, Guid hospitalId)
        {
            var operation = await _context.AviationOperations
                .Include(o => o.AirUnit)
                .FirstOrDefaultAsync(o => o.Id == operationId);

            if (operation != null && operation.AirUnit != null)
            {
                var hospital = await _context.Hospitals.FindAsync(hospitalId);

                operation.AirUnit.Status = VehicleOperationalStatus.Transporting;
                _context.AirUnits.Update(operation.AirUnit);
                await _context.SaveChangesAsync();

                if (operation.IncidentId.HasValue)
                {
                    await _transportService.RecordAsync(new RecordTransportDto
                    {
                        IncidentId = operation.IncidentId.Value,
                        VehicleId = operation.AirUnit.Id,
                        VehicleType = "aviation",
                        VehicleLabel = operation.AirUnit.Callsign,
                        DestinationId = hospitalId,
                        DestinationName = hospital?.Name ?? "Nieznany szpital",
                        DestinationType = "hospital"
                    });
                }
            }
        }

        public async Task ReturnToBaseAsync(Guid operationId)
        {
            var operation = await _context.AviationOperations
                .Include(o => o.AirUnit)
                .FirstOrDefaultAsync(o => o.Id == operationId);

            if (operation != null && operation.AirUnit != null)
            {
                operation.AirUnit.Status = VehicleOperationalStatus.ReturningToBase;
                _context.AirUnits.Update(operation.AirUnit);
                await _context.SaveChangesAsync();
            }
        }

        public async Task EndOperationAsync(Guid operationId)
        {
            var operation = await _context.AviationOperations.Include(o => o.AirUnit).FirstOrDefaultAsync(o => o.Id == operationId);
            if (operation != null)
            {
                if (operation.AirUnit != null)
                {
                    await FreeUnitAsync(operation.AirUnit.Id);
                }
                else
                {
                    operation.EndTime = DateTime.UtcNow;
                    _context.AviationOperations.Update(operation);
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task AppendAviationWeatherAsync(Incident incident)
        {
            if (incident.Latitude == 0 && incident.Longitude == 0) return;
            if (incident.WeatherCondition?.Contains("Lot:", StringComparison.OrdinalIgnoreCase) == true) return;

            try
            {
                var flight = await _weatherService.GetFlightConditionsAsync(incident.Latitude, incident.Longitude);
                var lotInfo = $"Lot: {flight.FlightRules} ({flight.StationIcao})";
                if (!string.IsNullOrWhiteSpace(flight.RawMetar))
                    lotInfo += $"\nMETAR: {flight.RawMetar}";

                incident.WeatherCondition = string.IsNullOrWhiteSpace(incident.WeatherCondition)
                    ? lotInfo
                    : $"{incident.WeatherCondition.Trim()}\n{lotInfo}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AviationService] Nie udało się zapisać pogody lotniczej: {ex.Message}");
            }
        }
    }
}