using System;
using System.ComponentModel.DataAnnotations;

namespace FireDepartment;
public class FireEquipment
{
	[Key]
	public Guid Id { get; set; }
	public string Name { get; set; }

	public Guid FireTRuckId { get; set; }
	public virtual FireTruck Firetruck { get; set; }

    public FireEquipment()
	{
	}
}
