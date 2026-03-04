using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace FireDepartment;
public class Fireman
{
	[Key]
    public Guid Id { get; set; }
	public string Name { get; set; }
	public string Surname { get; set; }
	public string BadgeNumber { get; set; }
	public string Rank { get; set; }

    public Guid FDepartmentId { get; set; }
    public virtual FDepartment FDepartment { get; set; }

    public string IdentityUserId { get; set; }
    public virtual IdentityUser IdentityUser { get; set; }
	public ICollection<FireDepartmentOperation> FireDepartmentOperations { get; set; } = new List<FireDepartmentOperation>(); // Dodaj kolekcję operacji straży pożarnej

    public Fireman()
	{
	}
}
