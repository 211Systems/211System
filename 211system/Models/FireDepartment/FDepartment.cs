using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace FireDepartment;
public class FDepartment
{
	[Key]
	public Guid PDepartmentId { get; set; }
	public string Name { get; set; }
	public string Address { get; set; }
	public string District { get; set; }
	public ICollection<Fireman> Firemen { get; set; }

    public FDepartment()
	{
	}
}
