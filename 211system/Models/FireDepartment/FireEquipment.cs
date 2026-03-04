using System;
using System.ComponentModel.DataAnnotations;

public class FireEquipment
{
	[Key]
	public Guid Id { get; set; }
	public string Name { get; set; }

	public Guid PoliceCarId { get; set; }
	public virtual PoliceCar PoliceCar { get; set; }

    public FireEquipment()
	{
	}
}
