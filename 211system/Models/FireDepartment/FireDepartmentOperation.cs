using System;
using System.ComponentModel.DataAnnotations;

namespace FireDepartment;
public class FireDepartmentOperation
{
	[Key]
	public Guid Id { get; set; }
	public DateTime StartTime { get; set; } = DateTime.Now;
	public DateTime EndTime { get; set; }

	public Guid FDepartmentId { get; set; }
	public virtual FDepartment Department { get; set; }

	public Guid IncidentId { get; set; }
	//public virtual Incident Incident { get; set; }


    public FireDepartmentOperation()
	{
	}
}
