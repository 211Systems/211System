using System;
using System.ComponentModel.DataAnnotations;
using CPR112.Models;

namespace Police;
public class PoliceOperation
{
	[Key]
	public Guid Id { get; set; }
	public DateTime StartTime { get; set; } = DateTime.Now;
	public DateTime EndTime { get; set; }

	public Guid PDepartmentId { get; set; }
	public virtual PDepartment Department { get; set; }

	public Guid IncidentId { get; set; }
	public virtual Incident Incident { get; set; }


    public PoliceOperation()
	{
	}
}
