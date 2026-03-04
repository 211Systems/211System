using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace CPR112.Models;
public class Incident
{
    [Key]
    public Guid Id { get; set; }
    public string Description { get; set; }
    public DateTime ReportDate { get; set; } = DateTime.Now;
    public string Status { get; set; } 
    public Guid LocationId { get; set; }
    public Location Location { get; set; }
    public Guid OperatorId { get; set; }
    public Operator112 Operator { get; set; }
    
    public ICollection<DispatcherComment> Comments { get; set; } = new List<DispatcherComment>();
}