namespace _211system.Models
{
    public enum VehicleOperationalStatus
    {
        InBase = 0,
        EnRouteToIncident = 1,
        OnScene = 2,
        Transporting = 3,
        ReturningToBase = 4,
        TransportingToHospital = 5
    }

    public class UpdateLocationDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int? Status { get; set; }
    }
}