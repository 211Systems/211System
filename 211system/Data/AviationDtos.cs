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
}