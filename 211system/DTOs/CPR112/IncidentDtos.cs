using _211system.Models.Dtos;
using _211system.Models;

namespace _211system.DTOs.CPR112
{
    public class CreateIncidentDto
    {
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; 
        public Guid LocationId { get; set; }
        public Guid? OperatorId { get; set; }
    }

    public class IncidentDto : CreateIncidentDto
    {
        public Guid Id { get; set; }
        public string IncidentNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ReportedAt { get; set; }
    }

    public class ChangeIncidentStatusDto
    {
        public string NewStatus { get; set; } = string.Empty;
        public Guid OperatorId { get; set; }
    }
}