using _211system.Data;
using _211system.Models;
using _211system.Models.Aviation;
using _211system.Models.Dtos.Aviation;
using _211system.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace _211system.Services
{
    public class AviationService : IAviationService
    {
        private readonly _211DbContext _context;

        public AviationService(_211DbContext context)
        {
            _context = context;
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

        public async Task<IEnumerable<AirUnit>> GetAllAirUnitsAsync()
        {
            return await _context.AirUnits.Include(u => u.Airbase).ToListAsync();
        }

        public async Task AssignAirUnitToIncidentAsync(Guid unitId, Guid incidentId)
        {
            var unit = await _context.AirUnits.FindAsync(unitId);
            if (unit == null || !unit.IsAvailable) throw new InvalidOperationException("Jednostka jest niedostępna.");

            unit.IsAvailable = false;
            unit.CurrentIncidentId = incidentId;
            unit.Status = VehicleOperationalStatus.EnRouteToIncident;

            var incident = await _context.Incidents.FindAsync(incidentId);
            if (incident != null && incident.Status == "Nowe")
            {
                incident.Status = "W toku";
                _context.Incidents.Update(incident);
            }

            _context.AirUnits.Update(unit);
            await _context.SaveChangesAsync();
        }

        public async Task ReturnToBaseAsync(Guid unitId)
        {
            var unit = await _context.AirUnits.FindAsync(unitId);
            if (unit == null) throw new ArgumentException("Jednostka nie istnieje.");

            unit.Status = VehicleOperationalStatus.ReturningToBase;
            _context.AirUnits.Update(unit);
            await _context.SaveChangesAsync();
        }
    }
}