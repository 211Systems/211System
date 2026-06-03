using System.Globalization;
using _211system.Data;
using _211system.DTOs.CPR112;
using _211system.Models;
using CPR112.Models;
using Microsoft.EntityFrameworkCore;

namespace _211system.Services
{
    public class IncidentService : IIncidentService
    {
        private readonly _211DbContext _context;

        public IncidentService(_211DbContext context)
        {
            _context = context;
        }

        public async Task<IncidentDto> CreateIncidentAsync(CreateIncidentDto dto)
        {
            var now = DateTime.UtcNow;
            if (!await _context.IncidentTypes.AnyAsync(t => t.Id == dto.IncidentTypeId))
                throw new ArgumentException("Nieprawidłowy typ zgłoszenia.");

            var todayIncidentsCount = await _context.Incidents
                .Where(i => i.ReportDate.Date == now.Date)
                .CountAsync();

            var incidentNumber = $"112/{now:yyyy/MM/dd}/{(todayIncidentsCount + 1):D3}";

            double lat = 0;
            double lon = 0;
            if (!string.IsNullOrEmpty(dto.Latitude))
                lat = double.Parse(dto.Latitude.Replace(',', '.'), CultureInfo.InvariantCulture);

            if (!string.IsNullOrEmpty(dto.Longitude))
                lon = double.Parse(dto.Longitude.Replace(',', '.'), CultureInfo.InvariantCulture);

            var incident = new Incident
            {
                Id = Guid.NewGuid(),
                IncidentNumber = incidentNumber,
                Description = dto.Description,
                Status = "Nowe",
                SeverityLevelId = dto.SeverityLevelId,
                IncidentTypeId = dto.IncidentTypeId,
                ReportDate = now,
                Latitude = lat,
                Longitude = lon,
                OperatorId = dto.OperatorId,
                PhotoUrl = dto.PhotoUrl
            };

            await _context.Incidents.AddAsync(incident);
            await _context.SaveChangesAsync();

            await _context.Entry(incident).Reference(i => i.SeverityLevel).LoadAsync();
            await _context.Entry(incident).Reference(i => i.IncidentType).LoadAsync();

            return new IncidentDto
            {
                Id = incident.Id,
                IncidentNumber = incident.IncidentNumber,
                Description = incident.Description,
                Status = incident.Status,
                Severity = incident.SeverityLevel != null ? incident.SeverityLevel.Name : "Brak",
                IncidentType = incident.IncidentType != null ? incident.IncidentType.Name : "Brak",
                ReportedAt = incident.ReportDate,
                Latitude = incident.Latitude,
                Longitude = incident.Longitude,
                OperatorId = incident.OperatorId,
                PhotoUrl = incident.PhotoUrl
            };
        }

        public async Task<IncidentDto> GetIncidentByIdAsync(Guid id)
        {
            var incident = await _context.Incidents
                .Include(i => i.SeverityLevel)
                .Include(i => i.IncidentType)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (incident == null) throw new ArgumentException("Nie znaleziono zgłoszenia.");

            return new IncidentDto
            {
                Id = incident.Id,
                IncidentNumber = incident.IncidentNumber,
                Description = incident.Description,
                Status = incident.Status,
                Severity = incident.SeverityLevel != null ? incident.SeverityLevel.Name : "Brak",
                IncidentType = incident.IncidentType != null ? incident.IncidentType.Name : "Brak",
                ReportedAt = incident.ReportDate,
                Latitude = incident.Latitude,
                Longitude = incident.Longitude,
                OperatorId = incident.OperatorId,
                PhotoUrl = incident.PhotoUrl
            };
        }

        public async Task ChangeIncidentStatusAsync(Guid id, Guid operatorId, ChangeIncidentStatusDto dto)
        {
            var incident = await _context.Incidents.FindAsync(id);
            if (incident == null) throw new ArgumentException("Nie znaleziono zgłoszenia.");

            if (incident.Status == dto.NewStatus && incident.SeverityLevelId == dto.NewSeverityLevelId && string.IsNullOrEmpty(dto.NewPhotoUrl))
            {
                throw new InvalidOperationException("Zgłoszenie posiada już te parametry.");
            }

            if (incident.Status != dto.NewStatus)
            {
                var historyLog = new IncidentStatusHistory
                {
                    Id = Guid.NewGuid(),
                    IncidentId = incident.Id,
                    OldStatus = incident.Status,
                    NewStatus = dto.NewStatus,
                    ChangedAt = DateTime.UtcNow,
                    OperatorId = operatorId
                };
                await _context.IncidentStatusHistories.AddAsync(historyLog);
                incident.Status = dto.NewStatus;
            }

            if (dto.NewSeverityLevelId.HasValue)
            {
                incident.SeverityLevelId = dto.NewSeverityLevelId.Value;
            }

            if (!string.IsNullOrEmpty(dto.NewPhotoUrl))
            {
                incident.PhotoUrl = dto.NewPhotoUrl;
            }

            _context.Incidents.Update(incident);
            await _context.SaveChangesAsync();
        }
    }
}