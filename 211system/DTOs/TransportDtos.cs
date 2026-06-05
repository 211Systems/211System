using System;

namespace _211system.DTOs
{
    public class RecordTransportDto
    {
        public Guid IncidentId { get; set; }
        public Guid VehicleId { get; set; }
        public string VehicleType { get; set; }
        public string VehicleLabel { get; set; }
        public Guid DestinationId { get; set; }
        public string DestinationName { get; set; }
        public string DestinationType { get; set; }
    }
}
