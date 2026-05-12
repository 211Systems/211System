using _211system.Models.Hospital;

namespace _211system.DTOs.Hospital
{
    public class CreateHospitalDto
    {
        public string Name { get; set; }
        public bool HasSOR { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double OperatingRadiusKm { get; set; }
        public bool HasHelipad { get; set; } = false;

    }

    public class HospitalDto : CreateHospitalDto
    {
        public Guid Id { get; set; }
    }

    public class CreateAmbulanceDto
    {
        public AmbulanceType Type { get; set; }
        public string LicensePlate { get; set; }
        public Guid HospitalId { get; set; }
        public Guid? ParamedicId { get; set; }
    }

    public class AmbulanceDto : CreateAmbulanceDto
    {
        public Guid Id { get; set; }
        public bool IsAvailable { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Status { get; set; }

    }

    public class CreateParamedicDto
    {
        public string Name { get; set; }
        public string LastName { get; set; }
        public string LicenseNumber { get; set; }
        public string Specialization { get; set; }
        public string Email { get; set; }
        public string Rank { get; set; }
        public Guid HospitalId { get; set; }
        public string ParaAccountId { get; set; }
    }

    public class ParamedicDto : CreateParamedicDto
    {
        public Guid Id { get; set; }
        public string? TemporaryPassword { get; set; }
    }
}