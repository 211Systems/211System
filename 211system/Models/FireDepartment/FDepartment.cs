using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace FireDepartment;
public class FDepartment
{
	[Key]
	public Guid FDepartmentId { get; set; }
	public string Name { get; set; }
	public string Address { get; set; }
	public string District { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double OperatingRadiusKm { get; set; } = 15.0;
    public ICollection<Fireman> Firemen { get; set; }

    public FDepartment()
	{
	}
}
