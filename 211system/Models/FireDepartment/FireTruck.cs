using System;
using System.ComponentModel.DataAnnotations;

namespace FireDepartment;
public class FireTruck
{
    [Key]
	public Guid Id { get; set; }
	public string LicensePlate { get; set; }

    public Guid PDepartmentId { get; set; }
    public virtual FDepartment Department { get; set; }

    public Guid PoliceEquipmentId { get; set; }
    public virtual PoliceEquipment PoliceEquipment { get; set; }

    public FireTruck()
	{
	}
}
