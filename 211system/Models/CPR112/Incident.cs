using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    public string Severity { get; set; }

    public DateTime ReportDate { get; set; } = DateTime.Now;

    [Required]
    public string Status { get; set; } = "Nowe";

    public string? PhotoUrl { get; set; }

    [Required]
    public Guid LocationId { get; set; }

    [ForeignKey("LocationId")]
    public virtual Enc Location { get; set; }

    public Guid? OperatorId { get; set; }

    [ForeignKey("OperatorId")]
    public virtual Operator112 Operator { get; set; }

    public bool IsPoliceActive { get; set; } = false;
    public bool IsFireActive { get; set; } = false;
    public bool IsMedicalActive { get; set; } = false;

    public virtual ICollection<DispatcherComment> Comments { get; set; } = new List<DispatcherComment>();
}