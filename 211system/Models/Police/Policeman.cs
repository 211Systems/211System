using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Police;
public class Policeman
{
	[Key]
	public Guid Id { get; set; }
	public string Name { get; set; }
	public string Surname { get; set; }
	public string BadgeNumber { get; set; }
	public string Rank { get; set; }
    public Guid PDepartmentId { get; set; }
    public PDepartment PDepartment { get; set; }

    public string IdentityUserId { get; set; }
    public virtual IdentityUser IdentityUser { get; set; }
	public ICollection<PoliceOperation> PoliceOperations { get; set; } = new List<PoliceOperation>(); // Dodaj kolekcję operacji policyjnych

    public Policeman()
	{
	}
}
