using System;
using System.ComponentModel.DataAnnotations;

namespace _211system.Models
{
    public class TransportRecord
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid IncidentId { get; set; }

        public Guid VehicleId { get; set; }

        [MaxLength(20)]
        public string VehicleType { get; set; }

        [MaxLength(100)]
        public string VehicleLabel { get; set; }

        public Guid DestinationId { get; set; }

        [MaxLength(200)]
        public string DestinationName { get; set; }

        [MaxLength(30)]
        public string DestinationType { get; set; }

        public DateTime TransportedAt { get; set; } = DateTime.UtcNow;
    }
}
