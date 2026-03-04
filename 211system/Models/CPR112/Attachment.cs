using System.ComponentModel.DataAnnotations;

namespace CPR112.Models;

public class Attachment {
    [Key]
    public Guid Id { get; set; }
    public string PathToFile { get; set; }
    public Guid IncidentId { get; set; }
    public Incident Incident { get; set; }
}