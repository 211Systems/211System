using _211system.Models.Aviation;

namespace _211system.Models.Dtos.Aviation
{
    public class CreateAirbaseDto
    {
        public string Name { get; set; }
        public string IcaoCode { get; set; }
        public ServiceType ServiceType { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class CreateAirUnitDto
    {
        public string Callsign { get; set; }
        public AirUnitType Type { get; set; }
        public ServiceType ServiceType { get; set; }
        public Guid AirbaseId { get; set; }
    }
    public class AirUnitDto
    {
        public Guid Id { get; set; }
        public string Callsign { get; set; }
        public int Type { get; set; }
        public int ServiceType { get; set; }
        public bool IsAvailable { get; set; }
        public int Status { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public Guid AirbaseId { get; set; }
    }
    public class UpdateAirUnitDto
    {
        public string Callsign { get; set; }
        public AirUnitType Type { get; set; }
        public Guid AirbaseId { get; set; }
    }
}