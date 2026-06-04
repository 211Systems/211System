using _211system.Models.Hospital;

namespace _211system.DTOs.Hospital
{
    public class UpdateHospitalDto
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public bool HasSOR { get; set; }
        public bool HasHelipad { get; set; }
    }

    public class UpdateAmbulanceDto
    {
        public string LicensePlate { get; set; }
        public AmbulanceType Type { get; set; }
        public Guid? ParamedicId { get; set; }
    }

    public class CreateAmbulanceEquipmentDto
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
    }

    public class AmbulanceEquipmentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public Guid AmbulanceId { get; set; }
    }
    public class UpdateParamedicDto
    {
        public string Name { get; set; }
        public string LastName { get; set; }
        public string LicenseNumber { get; set; }
        public string Rank { get; set; }
    }
    public class MedicalOperationDto
    {
        public Guid Id { get; set; }
        public Guid ParamedicId { get; set; }
        public string ParamedicName { get; set; }
        public Guid ReportId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
    public class IncidentDetailsMedicDto
    {
        public string IncidentNumber { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
        public string Status { get; set; }
        public DateTime ReportDate { get; set; }
        public string Address { get; set; }
        public string IncidentType { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}