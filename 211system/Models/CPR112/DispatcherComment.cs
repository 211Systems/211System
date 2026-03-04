using System.ComponentModel.DataAnnotations;

namespace CPR112.Models;

public class DispatcherComment {
    [Key]
    public Guid Id { get; set; }
    public string Content { get; set; }
    public DateTime AddDate { get; set; } = DateTime.Now;
    public Guid IncidentId { get; set; }
    public Incident Incident { get; set; }
}