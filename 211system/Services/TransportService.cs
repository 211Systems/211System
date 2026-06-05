using _211system.Data;
using _211system.DTOs;
using _211system.Models;

namespace _211system.Services
{
    public interface ITransportService
    {
        Task RecordAsync(RecordTransportDto dto);
    }

    public class TransportService : ITransportService
    {
        private readonly _211DbContext _context;

        public TransportService(_211DbContext context)
        {
            _context = context;
        }

        public async Task RecordAsync(RecordTransportDto dto)
        {
            if (dto.IncidentId == Guid.Empty || dto.VehicleId == Guid.Empty || dto.DestinationId == Guid.Empty)
                throw new ArgumentException("Brak wymaganych danych transportu.");

            var record = new TransportRecord
            {
                IncidentId = dto.IncidentId,
                VehicleId = dto.VehicleId,
                VehicleType = (dto.VehicleType ?? "").ToLowerInvariant(),
                VehicleLabel = dto.VehicleLabel ?? "",
                DestinationId = dto.DestinationId,
                DestinationName = dto.DestinationName ?? "",
                DestinationType = dto.DestinationType ?? "hospital",
                TransportedAt = DateTime.UtcNow
            };

            await _context.TransportRecords.AddAsync(record);
            await _context.SaveChangesAsync();
        }
    }
}
