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

    public Guid FireEquipmentid { get; set; }
    public virtual ICollection<FireEquipment> FireEquipments { get; set; } = new List<FireEquipment>();
    public FireTruck()
	{
	}
}
