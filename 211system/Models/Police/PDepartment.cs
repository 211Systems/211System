using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Police;
public class PDepartment
{
	[Key]
    public Guid PDepartmentId { get; set; }
	public string Name { get; set; }
	public string Address { get; set; }
	public string District { get; set; }
	public ICollection<Policeman> Policemen { get; set; }

    public PDepartment()
	{
	}
}
