using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CPR112.Models;

namespace FireDepartment;

public class FireDepartmentOperation
{
    [Key]
    public Guid Id { get; set; }
    
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }

    public Guid FDepartmentId { get; set; }
    public virtual FDepartment Department { get; set; }

    public Guid IncidentId { get; set; }
    public virtual Incident Incident { get; set; }

    public Guid? FiremanId { get; set; }
    
    [ForeignKey(nameof(FiremanId))]
    public virtual Fireman Fireman { get; set; }

    public FireDepartmentOperation()
    {
    }
}