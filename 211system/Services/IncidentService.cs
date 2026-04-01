using _211system.Data;
using _211system.DTOs.CPR112;
using Microsoft.EntityFrameworkCore;
using CPR112.Models;

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

            var todayIncidentsCount = await _context.Incidents
                .Where(i => i.ReportDate.Date == now.Date)
                .CountAsync();

            var incidentNumber = $"112/{now:yyyy/MM/dd}/{(todayIncidentsCount + 1):D3}";

            var incident = new Incident 
            {
                Id = Guid.NewGuid(),
                IncidentNumber = incidentNumber,
                Description = dto.Description,
                Status = "Nowe",
                Severity = dto.Severity,
                ReportDate = now,
                LocationId = dto.LocationId,
                OperatorId = dto.OperatorId,
                PhotoUrl = dto.PhotoUrl 
            };

            await _context.Incidents.AddAsync(incident);
            await _context.SaveChangesAsync();

            return new IncidentDto
            {
                Id = incident.Id,
                IncidentNumber = incident.IncidentNumber,
                Description = incident.Description,
                Status = incident.Status,
                Severity = incident.Severity,
                ReportedAt = incident.ReportDate,
                LocationId = incident.LocationId,
                OperatorId = incident.OperatorId,
                PhotoUrl = incident.PhotoUrl
            };
        }

        public async Task<IncidentDto> GetIncidentByIdAsync(Guid id)
        {
            var incident = await _context.Incidents.FindAsync(id);
            if (incident == null) throw new ArgumentException("Nie znaleziono zgłoszenia.");

            return new IncidentDto
            {
                Id = incident.Id,
                IncidentNumber = incident.IncidentNumber,
                Description = incident.Description,
                Status = incident.Status,
                Severity = incident.Severity,
                ReportedAt = incident.ReportDate,
                LocationId = incident.LocationId,
                OperatorId = incident.OperatorId,
                PhotoUrl = incident.PhotoUrl
            };
        }

        public async Task ChangeIncidentStatusAsync(Guid id, Guid operatorId, ChangeIncidentStatusDto dto)
        {
            var incident = await _context.Incidents.FindAsync(id);
            if (incident == null) throw new ArgumentException("Nie znaleziono zgłoszenia.");

            if (incident.Status == dto.NewStatus && incident.Severity == dto.NewSeverity && string.IsNullOrEmpty(dto.NewPhotoUrl))
            {
                throw new InvalidOperationException("Zgłoszenie posiada już te parametry.");
            }

            if (incident.Status != dto.NewStatus)
            {
                var historyLog = new StatusHistory
                {
                    Id = Guid.NewGuid(),
                    IncidentId = incident.Id,
                    OldStatus = incident.Status,
                    NewStatus = dto.NewStatus,
                    ChangeDate = DateTime.UtcNow,
                    OperatorId = operatorId
                };
                await _context.StatusHistories.AddAsync(historyLog);
                incident.Status = dto.NewStatus;
            }

            incident.Severity = dto.NewSeverity;

            if (!string.IsNullOrEmpty(dto.NewPhotoUrl))
            {
                incident.PhotoUrl = dto.NewPhotoUrl;
            }

            _context.Incidents.Update(incident);
            await _context.SaveChangesAsync();
        }
    }
}