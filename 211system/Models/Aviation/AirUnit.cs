using System;

namespace _211system.Models.Aviation
{
    public class AirUnit
    {
        public Guid Id { get; set; }
        public string Callsign { get; set; }
        public AirUnitType Type { get; set; }
        public ServiceType ServiceType { get; set; }

        public Guid AirbaseId { get; set; }
        public Airbase Airbase { get; set; }

        public bool IsAvailable { get; set; }
        public VehicleOperationalStatus Status { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public Guid? CurrentIncidentId { get; set; }
    }
}