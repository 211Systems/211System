using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Police;
using FireDepartment;
using _211system.Models.Hospital;


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
    public Guid Operator112Id { get; set; }
    public Operator112 Operator112 { get; set; }
    public FinalReport FinalReport { get; set; }
    
    public ICollection<DispatcherComment> Comments { get; set; } = new List<DispatcherComment>();
    public ICollection<PoliceOperation> PoliceOperations { get; set; } = new List<PoliceOperation>();
    public ICollection<FireDepartmentOperation> FireDepartmentOperations { get; set; } = new List<FireDepartmentOperation>();
    public ICollection<MedicalOperation> MedicalOperations { get; set; } = new List<MedicalOperation>();
}