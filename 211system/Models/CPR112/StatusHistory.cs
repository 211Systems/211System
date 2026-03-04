using System.ComponentModel.DataAnnotations;

namespace CPR112.Models;

public class StatusHistory {
    [Key]
    public Guid Id { get; set; }
    public string OldStatus { get; set; }
    public string NewStatus { get; set; }
    public DateTime ChangeDate { get; set; }
    public Guid IncidentId { get; set; }
    public Guid OperatorId { get; set; }
}