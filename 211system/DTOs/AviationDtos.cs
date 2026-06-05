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
        public string AirbaseName { get; set; }
        public Guid? CurrentIncidentId { get; set; }
        public Guid? PilotId { get; set; }
        public string PilotName { get; set; }
        public List<AirCrewMemberDto> Crew { get; set; } = new();
    }

    public class AirCrewMemberDto
    {
        public Guid MemberId { get; set; }
        public string MemberName { get; set; }
    }

    public class AssignCrewDto
    {
        public List<AirCrewMemberDto> Crew { get; set; } = new();
    }
    public class UpdateAirUnitDto
    {
        public string Callsign { get; set; }
        public AirUnitType Type { get; set; }
        public Guid AirbaseId { get; set; }
    }

    public class AssignPilotDto
    {
        public Guid? PilotId { get; set; }
        public string PilotName { get; set; }
    }
}