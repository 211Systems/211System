using _211system.Models.Dtos;
using _211system.Models;

namespace _211system.DTOs.CPR112
{
    public class CreateIncidentDto
    {
        public string Description { get; set; }
        public int SeverityLevelId { get; set; }
        public int IncidentTypeId { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public Guid? OperatorId { get; set; }
        public string? PhotoUrl { get; set; }
    }

    public class IncidentDto
    {
        public Guid Id { get; set; }
        public string IncidentNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string IncidentType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ReportedAt { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public Guid? OperatorId { get; set; }
        public string? PhotoUrl { get; set; }
        public int AttachmentCount { get; set; }
    }

    public class ChangeIncidentStatusDto
    {
        public string NewStatus { get; set; } = string.Empty;

        public int? NewSeverityLevelId { get; set; }

        public Guid OperatorId { get; set; }
        public string? NewPhotoUrl { get; set; }
    }
}