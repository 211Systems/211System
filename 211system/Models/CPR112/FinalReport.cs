using System.ComponentModel.DataAnnotations;

namespace CPR112.Models;

public class FinalReport {
    [Key]
    public Guid Id { get; set; }
    public string Summary { get; set; }
    public Guid IncidentId { get; set; }
    public Incident Incident { get; set; }
}