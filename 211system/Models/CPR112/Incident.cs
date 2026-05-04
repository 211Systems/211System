using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using _211system.Models;

namespace CPR112.Models;

public class Incident
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string IncidentNumber { get; set; }

    [Required]
    public string Description { get; set; }

    [Required]
    public int? SeverityLevelId { get; set; }
    public SeverityLevel SeverityLevel { get; set; }

    public int? IncidentTypeId { get; set; }
    public IncidentType IncidentType { get; set; }

    public DateTime ReportDate { get; set; } = DateTime.Now;

    [Required]
    public string Status { get; set; } = "Nowe";

    public string? PhotoUrl { get; set; }

    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }

    public Guid? OperatorId { get; set; }

    [ForeignKey("OperatorId")]
    public virtual Operator112 Operator { get; set; }

    public bool IsPoliceActive { get; set; } = false;
    public bool IsFireActive { get; set; } = false;
    public bool IsMedicalActive { get; set; } = false;

    public virtual ICollection<DispatcherComment> Comments { get; set; } = new List<DispatcherComment>();

    public ICollection<IncidentStatusHistory> StatusHistories { get; set; } = new List<IncidentStatusHistory>();
}